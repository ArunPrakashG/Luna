using System;
using System.Collections.Generic;
using System.Text;

namespace Luna.Modules {
	public struct EventResponse {
		public readonly string? ResponseMessage;
		public readonly bool IsSuccess;

		public EventResponse(string? resp, bool isSuccess) {
			ResponseMessage = resp;
			IsSuccess = isSuccess;
		}
	}
}
