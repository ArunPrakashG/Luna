using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assistant.Rest {
	public struct RequestParameter {
		public readonly string? AuthToken;
		public readonly string? PublicIp;
		public readonly string? LocalIp;
		public readonly DateTime RequestTime;
		public readonly object[] Parameters;

		public RequestParameter(string? token, string? publicIp, string? localIp, object[] parameters = null) {
			AuthToken = token;
			PublicIp = publicIp;
			LocalIp = localIp;
			RequestTime = DateTime.Now;
			Parameters = parameters;
		}
	}
}
