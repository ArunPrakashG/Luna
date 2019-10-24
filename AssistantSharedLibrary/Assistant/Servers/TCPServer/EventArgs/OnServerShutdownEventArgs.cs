using System;
using System.Collections.Generic;
using System.Text;

namespace AssistantSharedLibrary.Assistant.Servers.TCPServer.EventArgs {
	public class OnServerShutdownEventArgs {
		public readonly DateTime ShutdownTime;
		public readonly bool ReconnectAllowed;

		public OnServerShutdownEventArgs(DateTime dt, bool recc) {
			ShutdownTime = dt;
			ReconnectAllowed = recc;
		}
	}
}
