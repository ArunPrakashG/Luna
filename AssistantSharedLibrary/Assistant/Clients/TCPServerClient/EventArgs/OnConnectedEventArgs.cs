using System;
using System.Collections.Generic;
using System.Text;

namespace AssistantSharedLibrary.Assistant.Clients.TCPServerClient.EventArgs {
	public class OnConnectedEventArgs {
		public readonly DateTime ConnectedTime;
		public readonly string ServerIp;
		public readonly int ServerPort;

		public OnConnectedEventArgs(DateTime dt, string serverIp, int port) {
			ConnectedTime = dt;
			ServerIp = serverIp;
			ServerPort = port;
		}
	}
}
