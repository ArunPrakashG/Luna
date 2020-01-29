using Newtonsoft.Json;

namespace Assistant.Servers.TCPServer.Commands {
	public class SetRemainder {
		[JsonProperty]
		public string RemainderMessage { get; set; }

		[JsonProperty]
		public int RemainderDelay { get; set; }

		public SetRemainder(string _msg, int _delay) {
			RemainderMessage = _msg;
			RemainderDelay = _delay;
		}

		public string AsJson() => JsonConvert.SerializeObject(this);
	}
}
