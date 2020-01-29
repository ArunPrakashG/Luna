using Assistant.AssistantCore;
using Assistant.Extensions;
using Newtonsoft.Json;

namespace Assistant.Servers.TCPServer.Responses {
	public class AssistantInfoResponse {
		[JsonProperty]
		public bool InitiationCompleted => Core.CoreInitiationCompleted;

		[JsonProperty]
		public bool IsNetworkAvailable => Core.IsNetworkAvailable;

		[JsonProperty]
		public string PublicIP => Constants.ExternelIP;

		[JsonProperty]
		public string LocalIP => Constants.LocalIP;

		[JsonProperty]
		public bool GpioControlAllowed => !Core.IsUnknownOs && Core.Config.EnableGpioControl;

		public string AsJson() => JsonConvert.SerializeObject(this);
	}
}
