using AssistantSharedLibrary.Assistant.Servers.TCPServer.Requests;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Server.CoreServer.EventArgs {
	public class OnReceivedEventArgs {
		public readonly string ReceivedRaw = string.Empty;
		public readonly DateTime ReceivedTime;
		public readonly BaseRequest BaseRequest;
		public readonly int Uid;
		public readonly string ReceivedFromAddress = string.Empty;

		public OnReceivedEventArgs(string _raw, DateTime dt, BaseRequest _base, int uid, string _fromIp) {
			ReceivedRaw = _raw;
			ReceivedTime = dt;
			BaseRequest = _base;
			Uid = uid;
			ReceivedFromAddress = _fromIp;
		}
	}
}
