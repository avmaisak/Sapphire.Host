using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gcode.Entity;
using Gcode.Utils;

namespace Sapphire.Host.ConsoleApp {
	public static class SapphireHostConsole {
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
		private static GcodeCommandFrame _lastSentCommand;
		private static string _response;
		private static void Restart() {
			if (_dev.ConnectedToDevice) {
				Disconnect();
				Connect();
				_totalSent = 1;
			}
		}
		private static void SendNext() {
			_lastSentCommand = CommandStack.Pop();
			var strCmd = ToDevOutput(_lastSentCommand);
			_dev.SendCommandFrame(strCmd);
			
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

				Console.WriteLine($">> {CommandStack.Count:D5} {GcodeParser.ToStringCommand(_lastSentCommand)} << {_response}");

				_totalSent++;
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
