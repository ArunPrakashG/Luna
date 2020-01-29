using Newtonsoft.Json;

namespace Assistant.Servers.TCPServer.Commands {
	public class GetGpio {
		[JsonProperty]
		public int PinNumber { get; set; }

		public GetGpio(int _pinNumber) {
			PinNumber = _pinNumber;
		}

		public string AsJson() => JsonConvert.SerializeObject(this);
	}
}
