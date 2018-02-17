using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gcode.Entity;
using Gcode.Utils;

namespace Sapphire.Host.ConsoleApp {
	public static class SapphireHostConsole {
		private static DateTime _jobStartTime;
		private const int BaudRate = 115200;
		private static RepitierFirmwareDevice _dev;
		private static string ToDevOutput(GcodeCommandFrame g) {
			g.N = _totalSent;
			var cmd = GcodeParser.ToStringCommand(g);
			var cmdStr = $"{cmd} *{GcodeCrc.FrameCrc(g)}";
			return cmdStr;
		}
		private static readonly Stack<GcodeCommandFrame> CommandStack = new Stack<GcodeCommandFrame>();
		private static long _totalSent = 1;
		private static long _totalAbs;
		private static GcodeCommandFrame _lastSentCommand;
		private static string _response;
		private static readonly string FileName = $@"C:\Users\User\Documents\{DateTime.Now.ToShortDateString()}.{DateTime.Now.ToShortTimeString().Replace(":", "_")}.txt";
		private static string OutputLog() {
			return $"{_totalAbs:D5}: { _totalAbs * 100 / CommandStack.Count:D2}% {Math.Round(Convert.ToDecimal((DateTime.Now - _jobStartTime).TotalSeconds), 2)} sec >> {GcodeParser.ToStringCommand(_lastSentCommand)} << {_response.Trim()}";
		}
		private static async void SaveToFile(string data) {

			if (CommandStack.Count > 0) {
				var bytes = Encoding.UTF8.GetBytes($"{data}\r\n");

				using (var stream = File.Open(FileName, FileMode.Append)) {
					while (!stream.CanWrite) { }
					await stream.WriteAsync(bytes, 0, bytes.Length);
				}
			}
		}
		private static void Restart() {
			if (_dev.ConnectedToDevice) {
				Disconnect();
				Connect();
				_totalSent = 1;
			}
		}
		private static void SendNext() {
			_jobStartTime = DateTime.Now;
			_lastSentCommand = CommandStack.Pop();
			_dev.SendCommandFrame(ToDevOutput(_lastSentCommand));
		}
		private static void GetResponse() {
			_response = _dev.GetResponse().Trim();
		}
		private static void DoJob() {

			while (CommandStack.Count > 0) {

				SendNext();
				GetResponse();

				if (_response.Contains("fatal")) {
					CommandStack.Push(_lastSentCommand);
					Restart();
					continue;
				}

				while (!_response.Contains("ok")) {
					_response = _dev.GetResponse().Trim();
				}

				Console.WriteLine(OutputLog());

				SaveToFile(OutputLog());

				_totalSent++;
				_totalAbs = _totalAbs + 1;
				_response = string.Empty;

			}
		}
		private static void Connect() {
			_dev.Init();
			_dev.Connect();
			_dev.ClearBuffers(1000);
		}
		private static void Disconnect() {
			_dev.Disconnect();
		}
		private static void PrepareJob(IReadOnlyList<string> commands) {
			var lines = commands.Count;
			for (var i = lines; i > 0; i--) {
				var line = commands[i - 1];
				if (line.StartsWith(";")) continue;
				var gCode = GcodeParser.ToGCode(line);
				if (!string.IsNullOrWhiteSpace(gCode.Comment)) {
					gCode.Comment = null;
				}
				//только для отладки.
				if (gCode.M != 109 && gCode.G != 28 && gCode.M != 190) {
					CommandStack.Push(gCode);
				}
			}
		}
		private static string[] ReadFile(string path) {
			return File.ReadAllLines(path).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
		}
		public static int Main(string[] args) {
			var path = args[0];
			var fileContent = ReadFile(path);
			PrepareJob(fileContent);

			using (_dev = new RepitierFirmwareDevice("COM4", BaudRate)) {
				Connect();
				DoJob();
				Disconnect();
			}
			return 0;
		}
	}
}
