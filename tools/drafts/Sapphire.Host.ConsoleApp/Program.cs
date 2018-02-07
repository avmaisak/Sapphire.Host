
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Gcode.Entity;
using Gcode.Utils;

namespace Sapphire.Host.ConsoleApp {
	public static class SapphireHostConsole {
		//private static readonly SapphireHost SapphireHost;
		//static SapphireHostConsole() {
		//	SapphireHost = SapphireHost.GetInstance();
		//}
		private const int BaudRate = 115200;
		private static RepitierFirmwareDevice _dev;
		private static IEnumerable<string> LoadFileContent(string path) {
			var fileExist = File.Exists(path);
			if (fileExist) {
				return File.ReadAllLines(path);
			}
			return null;
		}
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
		private static void Restart()
		{
			if (_dev.ConnectedToDevice)
			{
				Disconnect();
				Connect();
				_totalSent = 1;
			}
		}
		private static void SendNext() {
			_lastSentCommand = CommandStack.Pop();
			var strCmd = ToDevOutput(_lastSentCommand);
			_dev.SendCommandFrame(strCmd);
			Thread.Sleep( (Encoding.Unicode.GetByteCount(strCmd) * (10000) / BaudRate));
		}
		private static void HandleResponse()
		{
			if (_response.Contains("fatal")) {
				CommandStack.Push(_lastSentCommand);
				Restart();
				return;
			}
			while (!_response.Contains("ok")) {
				_response = _dev.GetResponse();
			}
		}
		private static void GetResponse()
		{
			_response = _dev.GetResponse();
		
		}
		private static void DoJob() {
			while (CommandStack.Count > 0) {

				SendNext();
				//GetResponse();
				//HandleResponse();

				_response = _dev.GetResponse().Trim();
				//Thread.Sleep(13 * 10000 / BaudRate * 100);
				//Thread.Sleep(Encoding.Unicode.GetByteCount(_response) * 10000 / BaudRate);

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
		private static void RealFileTestCube(string path) {
			var f = File.ReadAllLines(path).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
			var lines = f.Length;
			for (var i = lines; i > 0; i--) {
				var line = f[i - 1];
				if (!line.StartsWith(";")) {
					var gCode = GcodeParser.ToGCode(line);
					if (!string.IsNullOrWhiteSpace(gCode.Comment)) {
						gCode.Comment = null;
					}

					if (gCode.M != 109 && gCode.G != 28 && gCode.M != 190) {
						CommandStack.Push(gCode);
					}

				}
			}

			f = null;
			

		}
		public static int Main(string[] args) {
			using (_dev = new RepitierFirmwareDevice("COM5", BaudRate)) {
				Connect();
				RealFileTestCube(args[0]);
				DoJob();
				Disconnect();
			}
			return 0;
		}
	}
}
