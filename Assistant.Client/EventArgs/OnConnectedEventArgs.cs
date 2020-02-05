using System;

namespace Assistant.Client.EventArgs {
	public class OnConnectedEventArgs {
		public readonly DateTime ConnectedTime;
		public readonly string? ServerIp;
		public readonly int ServerPort;

		public OnConnectedEventArgs(DateTime dt, string? serverIp, int port) {
			ConnectedTime = dt;
			ServerIp = serverIp;
			ServerPort = port;
		}
	}
}
