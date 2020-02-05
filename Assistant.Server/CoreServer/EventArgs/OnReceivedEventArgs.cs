using Assistant.Server.CoreServer.Requests;
using System;

namespace Assistant.Server.CoreServer.EventArgs {
	public class OnReceivedEventArgs {
		public readonly string? ReceivedRaw = null;
		public readonly DateTime ReceivedTime;
		public readonly BaseRequest? BaseRequest;
		public readonly int Uid;
		public readonly string? ReceivedFromAddress = null;

		public OnReceivedEventArgs(string? _raw, DateTime dt, BaseRequest? _base, int uid, string? _fromIp) {
			ReceivedRaw = _raw;
			ReceivedTime = dt;
			BaseRequest = _base;
			Uid = uid;
			ReceivedFromAddress = _fromIp;
		}
	}
}
