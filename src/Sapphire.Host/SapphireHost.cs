using System;
using System.Collections.Generic;
using System.Linq;
using Gcode.Utils;

namespace Sapphire.Host {
	/// <summary>
	/// Хост
	/// </summary>
	public class SapphireHost {
		private HostConfiguration _hostConfiguration;
		private static readonly Lazy<SapphireHost> Lazy = new Lazy<SapphireHost>(() => new SapphireHost());
		private void InitConfig() {
			_hostConfiguration = new HostConfiguration { PortName = "COM5", BaudRate = 115200 };
		}
		private SapphireHost() {
			InitConfig();
		}
		public static SapphireHost GetInstance() {
			return Lazy.Value;
		}
	}
}