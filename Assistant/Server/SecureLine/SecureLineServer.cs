using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Assistant.Server.SecureLine {
	public class SecureLineServer {
		private TcpListener Listener { get; set; }
		private static int ServerPort { get; set; }
		private static IPAddress ListerningAddress { get; set; } = IPAddress.Any;


		
	}
}
