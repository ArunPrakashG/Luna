using System;
using System.Collections.Generic;
using System.Text;

namespace AssistantSharedLibrary.Assistant.Clients.TCPServerClient.EventArgs {
	public class OnDisconnectedEventArgs {
		public readonly DateTime DisconnectionTime;
		public readonly bool ReconnectInitiated;
		public readonly string ServerIp;
		public readonly int ServerPort;
		public readonly bool IsServerDisconnected;

		public OnDisconnectedEventArgs(DateTime dt, bool recc, string serverip, int port, bool serverDc) {
			DisconnectionTime = dt;
			ReconnectInitiated = recc;
			ServerIp = serverip;
			ServerPort = port;
			IsServerDisconnected = serverDc;
		}
	}
}
