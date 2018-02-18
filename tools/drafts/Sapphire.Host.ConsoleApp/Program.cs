using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gcode.Entity;
using Gcode.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;

[assembly: CLSCompliant(true)]
namespace Sapphire.Host.ConsoleApp {
	public static class SapphireHostConsole {
		private static JsonRequest _jsonRequest;
		private static readonly string AppPath = AppDomain.CurrentDomain.BaseDirectory;
		private static readonly string AppCfgPath = $"{AppPath}host.cfg.json";
		private static HostConfiguration GetCfg() {
			return JsonConvert.DeserializeObject<HostConfiguration>(ReadFileStr(AppCfgPath));
		}
		private static void SaveCfg(HostConfiguration cfg) {

			File.WriteAllText(AppCfgPath, JsonConvert.SerializeObject(cfg));
		}
		private static SapphireWebClient _webClient;
		private static void InitWebClient() {
			_webClient = new SapphireWebClient(GetCfg().DispatcherUrl);
		}
		private static void Register() {
			var resObj = _webClient.Post("Device", "Register", GetCfg()).GetAwaiter().GetResult();
			var res = JsonConvert.DeserializeObject<JsonResultViewModel>(resObj);
			var token = res.Obj.ToString();
			var cfg = new HostConfiguration();
			cfg = GetCfg();
			cfg.Token = token;
			SaveCfg(cfg);
			_jsonRequest = new JsonRequest { Token = cfg.Token };

		}
		private static HubConnection _connection;
		private static DateTime _jobStartTime;
		private const int BaudRate = 115200;
		private static RepitierFirmwareDevice _dev = new RepitierFirmwareDevice("");
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
		private static async void SendMsg(string message, string port = "Send") {
			await _connection.InvokeAsync<string>(port, message);
		}
		private static readonly string LogFileName = $@"{AppPath}{DateTime.Now.ToShortDateString()}.{DateTime.Now.ToShortTimeString().Replace(":", "_")}.txt";
		private static string OutputLog() {
			return $"{_totalAbs:D5}: { _totalAbs * 100 / CommandStack.Count:D2}% {Math.Round(Convert.ToDecimal((DateTime.Now - _jobStartTime).TotalSeconds), 2)} sec >> {GcodeParser.ToStringCommand(_lastSentCommand)} << {_response.Trim()}";
		}
		private static async void SaveLog(string data) {

			if (CommandStack.Count > 0) {
				var bytes = Encoding.UTF8.GetBytes($"{DateTime.Now.ToShortDateString()}:{DateTime.Now.ToShortTimeString()} [{data}]\r\n");
				using (var stream = File.Open(LogFileName, FileMode.Append)) {
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

				SendMsg(OutputLog());

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
		private static string ReadFileStr(string path) {
			return !File.Exists(path) ? string.Empty : File.ReadAllText(path);
		}
		private static string[] ReadFile(string path) {
			return File.ReadAllLines(path).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
		}
		private static async Task StartConnectionAsync() {
			_connection = new HubConnectionBuilder()
				.WithUrl("http://localhost:21912/Comm")
				.Build();

			await _connection.StartAsync();
		}
		private static async Task DisposeAsync() {
			await _connection.DisposeAsync();
		}
		private static void InitCfg() {
			if (!File.Exists(AppCfgPath)) {

				var c = new HostConfiguration {
					HostId = Guid.NewGuid().ToString(),
					AvailablePorts = RepitierFirmwareDevice.PortsAvailable,
					DispatcherUrl = "http://localhost:21912/"
				};

				SaveCfg(c);
			}
		}
		public static int Main(string[] args) {

			StartConnectionAsync().GetAwaiter().GetResult();

			InitCfg();
			InitWebClient();
			Register();

			//_connection.On<string>("Send", (message) => {
			//	SaveLog(message);
			//});

			SendMsg($"ok. Token recived. {_jsonRequest.Token}");

			var path = args[0];
			var fileContent = ReadFile(path);

			PrepareJob(fileContent);

			using (_dev = new RepitierFirmwareDevice("COM4", BaudRate)) {
				Connect();
				DoJob();
				Disconnect();
			}

			DisposeAsync().GetAwaiter().GetResult();
			return 0;
		}
	}
}