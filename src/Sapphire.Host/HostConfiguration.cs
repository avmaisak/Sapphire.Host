
using System.Collections.Generic;

namespace Sapphire.Host {
	public class HostConfiguration {
		public string HostId { get; set; }
		public int BaudRate { get; set; } = 115200;
		public string PortName { get; set; }
		public string[] AvailablePorts { get; set; }
		public string DispatcherUrl { get; set; }
		public string Token { get; set; }
		public ICollection<KeyValuePair<string, string>> Misc { get; set; }
	}
}
