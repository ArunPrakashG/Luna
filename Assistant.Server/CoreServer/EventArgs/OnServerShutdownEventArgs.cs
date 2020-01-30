using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Server.CoreServer.EventArgs {
	public class OnServerShutdownEventArgs {
		public readonly DateTime ShutdownTime;
		public readonly bool ReconnectAllowed;

		public OnServerShutdownEventArgs(DateTime dt, bool recc) {
			ShutdownTime = dt;
			ReconnectAllowed = recc;
		}
	}
}
