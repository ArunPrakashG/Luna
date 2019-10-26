using AssistantSharedLibrary.Assistant.Servers.TCPServer.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssistantSharedLibrary.Assistant.Clients.TCPServerClient.EventArgs {
	public class OnResponseReceivedEventArgs {
		public readonly DateTime ReceivedTime;
		public readonly BaseResponse ReceivedResponse;
		public readonly string ReceivedResponseUnparsed;

		public OnResponseReceivedEventArgs(DateTime dt, BaseResponse resp, string respUnparsed) {
			ReceivedTime = dt;
			ReceivedResponse = resp;
			ReceivedResponseUnparsed = respUnparsed;
		}
	}
}
