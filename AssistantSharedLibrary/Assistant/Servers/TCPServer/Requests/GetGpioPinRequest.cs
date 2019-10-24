using Newtonsoft.Json;

namespace AssistantSharedLibrary.Assistant.Servers.TCPServer.Requests {
	public class GetGpioPinRequest {
		[JsonProperty]
		public int PinNumber { get; set; }

		public GetGpioPinRequest(int _pinNumber) => PinNumber = _pinNumber;
	}
}
