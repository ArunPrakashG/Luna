using System;
using System.Collections.Generic;
using System.Text;

namespace AssistantSharedLibrary.Assistant.Servers.TCPServer.EventArgs {
	public class OnConnectedEventArgs {
		public readonly string IpAddress;
		public readonly DateTime ConnectionTime;
		public readonly string ClientUid;

		public OnConnectedEventArgs(string _ip, DateTime _connTime, string _uid) {
			IpAddress = _ip;
			ConnectionTime = _connTime;
			ClientUid = _uid;
		}
	}
}
