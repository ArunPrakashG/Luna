using Newtonsoft.Json;
using System;
using static Assistant.Server.CoreServer.CoreServerEnums;

namespace Assistant.Server.CoreServer.Requests {
	public class BaseRequest {
		[JsonIgnore]
		public int Identifier => !string.IsNullOrEmpty(RequestObject) ? RequestObject.GetHashCode() : RequestTime.GetHashCode();

		[JsonProperty]
		public DateTime RequestTime { get; set; } = DateTime.Now;

		[JsonProperty]
		public TYPE_CODE TypeCode { get; set; } = TYPE_CODE.UNKNOWN;

		[JsonProperty]
		public string RequestObject { get; set; } = string.Empty;

		public BaseRequest(string requestJson) {
			if (string.IsNullOrEmpty(requestJson)) {				
				return;
			}

			BaseRequest baseRequest = JsonConvert.DeserializeObject<BaseRequest>(requestJson);
			RequestTime = baseRequest.RequestTime;
			TypeCode = baseRequest.TypeCode;
			RequestObject = baseRequest.RequestObject;
		}

		public BaseRequest(DateTime reqTime, TYPE_CODE tCode, string reqObj) {
			RequestTime = reqTime;
			TypeCode = tCode;
			RequestObject = reqObj;
		}

		public static string SerializeRequest<TType>(TType type) where TType : class => JsonConvert.SerializeObject(type);

		public override int GetHashCode() {
			return base.GetHashCode();
		}

		public override bool Equals(object? obj) {
			if (obj == null) {
				return false;
			}

			BaseRequest request = (BaseRequest) obj;

			if (request.Identifier == Identifier) {
				return true;
			}

			return false;
		}
	}
}
