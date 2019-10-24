using System;
using System.Collections.Generic;
using System.Text;

namespace AssistantSharedLibrary.Assistant.Servers.TCPServer.EventArgs {
	public class OnDisconnectedEventArgs {
		public readonly string ClientUid;
		public readonly DateTime DisconnectedAt;
		public readonly bool IsReconnectInitiated;

		public OnDisconnectedEventArgs(string _uid, DateTime _dt, bool _reconnect) {
			ClientUid = _uid;
			DisconnectedAt = _dt;
			IsReconnectInitiated = _reconnect;
		}
	}
}
