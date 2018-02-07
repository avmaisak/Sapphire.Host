using Sapphire.Host.Core.Interfaces;

namespace Sapphire.Host.Core.Base
{
	/// <inheritdoc />
	public abstract class DeviceBase : IDevice {
		/// <inheritdoc />
		public abstract void Init();
		/// <inheritdoc />
		public abstract void Connect(int timeOut = 3000);
		/// <inheritdoc />
		public abstract void Disconnect(bool force = false);
		/// <inheritdoc />
		public abstract void SendCommandFrame(string frame);
		/// <inheritdoc />
		public abstract string GetResponse();
		/// <inheritdoc />
		public abstract void Dispose();
	}
}
