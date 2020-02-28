using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assistant.Rest {
	public struct RequestResponse {
		[JsonProperty]
		public readonly object Response;
		[JsonProperty]
		public readonly bool IsSuccess;
		[JsonProperty]
		public readonly DateTime ResponseTime;

		public RequestResponse(object resp, bool isSuccess) {
			Response = resp;
			IsSuccess = isSuccess;
			ResponseTime = DateTime.Now;
		}
	}
}
