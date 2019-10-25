using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using static AssistantSharedLibrary.Assistant.Enums;

namespace AssistantSharedLibrary.Assistant.Servers.TCPServer.Responses {
	public class BaseResponse {
		[JsonIgnore]
		public int Identifier => !string.IsNullOrEmpty(ResponseObject) ? ResponseObject.GetHashCode() : ResponseTime.GetHashCode();

		[JsonProperty]
		public DateTime ResponseTime { get; set; } = DateTime.Now;

		[JsonProperty]
		public TYPE_CODE TypeCode { get; set; } = TYPE_CODE.UNKNOWN;

		[JsonProperty]
		public string ResponseMessage { get; set; } = string.Empty;

		[JsonProperty]
		public string ResponseObject { get; set; } = string.Empty;

		public BaseResponse(DateTime respTime, TYPE_CODE typeCode, string respMsg, string respObj) {
			ResponseTime = respTime;
			TypeCode = typeCode;
			ResponseMessage = respMsg;
			ResponseObject = respObj;
		}

		public static string SerializeRequest<TType>(TType type) where TType : class => JsonConvert.SerializeObject(type);

		public static TType DeserializeRequest<TType>(string json) where TType : class => string.IsNullOrEmpty(json) ? default(TType) : JsonConvert.DeserializeObject<TType>(json);

		public override int GetHashCode() {
			return base.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj == null) {
				return false;
			}

			BaseResponse request = (BaseResponse) obj;

			if (request.Identifier == Identifier || (!string.IsNullOrEmpty(request.ResponseObject) && request.ResponseObject.Equals(ResponseObject, StringComparison.OrdinalIgnoreCase))) {
				return true;
			}

			return false;
		}
	}
}
