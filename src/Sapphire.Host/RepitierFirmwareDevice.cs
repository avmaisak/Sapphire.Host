using System;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using Sapphire.Host.Core.Base;

namespace Sapphire.Host {
	/// <inheritdoc />
	public sealed class RepitierFirmwareDevice : DeviceBase {

		private SerialPort _serialPort;
		private readonly string _portName;
		private readonly int _baudRate;
		public bool ConnectedToDevice { get; private set; }
		private bool _disposed;
		/// <summary>
		/// обновить информацию о состоянии соединения
		/// </summary>
		private void UpdateConnectionState() {
			ConnectedToDevice = _serialPort.IsOpen;
		}
		private int SendTimeOut(string commandFrame, int delay = 10000) {
			return Encoding.Unicode.GetByteCount(commandFrame) * delay / _baudRate;
		}
		/// <summary>
		/// очистка буферов
		/// </summary>
		/// <param name="timeout"></param>
		public void ClearBuffers(int timeout) {
			_serialPort.DiscardInBuffer();
			_serialPort.DiscardOutBuffer();
			Thread.Sleep(timeout);
		}
		public RepitierFirmwareDevice(string port, int baudRate = 15200) {
			_portName = port;
			_baudRate = baudRate;
		}
		private bool PortAvailable => SerialPort.GetPortNames().Contains(_portName);
		/// <inheritdoc />
		public override void Init() {
			if (PortAvailable) {
				_serialPort = new SerialPort(_portName, _baudRate) {
					Parity = Parity.None,
					StopBits = StopBits.One,
					DataBits = 8,
					DiscardNull = true,
					DtrEnable = true,
					RtsEnable = true
				};
			}

		}
		/// <inheritdoc />
		public override void Connect(int timeOut = 3000) {
			if (ConnectedToDevice) return;

			//открыть порт
			_serialPort.Open();
			Thread.Sleep(timeOut);

			//очистка буферов
			ClearBuffers(timeOut / 2);
			//обновить информацию о состоянии соединения
			UpdateConnectionState();

		}
		/// <inheritdoc />
		public override void Disconnect(bool force = false) {
			if (!ConnectedToDevice) return;
			_serialPort.Close();
			UpdateConnectionState();
		}
		/// <inheritdoc />
		public override void SendCommandFrame(string frame) {
			_serialPort.WriteLine(frame);
			Thread.Sleep(SendTimeOut(frame));
		}
		public override string GetResponse() {
			var res = string.Empty;
			var sb = new StringBuilder(res);

			while (_serialPort.BytesToRead > 0) {
				sb.Append(_serialPort.ReadTo("\r\n").Trim());
			}

			return sb.ToString();
		}
#pragma warning disable S2953 // Methods named "Dispose" should implement "IDisposable.Dispose"
		private void Dispose(bool disposing) {
			if (_disposed) return;
			if (disposing) {
				_serialPort.Dispose();
			}
			_disposed = true;
		}
		/// <inheritdoc />
		public override void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		~RepitierFirmwareDevice() {
			Dispose(false);
		}
	}
}