using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace AssistantSharedLibrary.Assistant.Servers.TCPServer.EventArgs {
	public class OnServerStartedListerningEventArgs {
		public readonly IPAddress ListerningAddress;
		public readonly int ServerPort;
		public readonly DateTime ListerningStartedAt;

		public OnServerStartedListerningEventArgs(IPAddress _add, int port, DateTime dt) {
			ListerningAddress = _add;
			ServerPort = port;
			ListerningStartedAt = dt;
		}
	}
}
