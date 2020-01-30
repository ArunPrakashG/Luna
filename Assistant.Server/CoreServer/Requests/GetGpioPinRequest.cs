using Newtonsoft.Json;

namespace Assistant.Server.CoreServer.Requests {
	public class GetGpioPinRequest {
		[JsonProperty]
		public int PinNumber { get; set; }

		public GetGpioPinRequest(int _pinNumber) => PinNumber = _pinNumber;
	}
}
