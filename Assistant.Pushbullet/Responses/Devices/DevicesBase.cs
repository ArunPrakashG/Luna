using Newtonsoft.Json;
using System;

namespace Assistant.Pushbullet.Responses.Devices {
	[Serializable]
	public class DevicesBase {
		[JsonProperty("active")]
		public bool Active { get; set; }

		[JsonProperty("app_version")]
		public int AppVersion { get; set; }

		[JsonProperty("created")]
		public float Created { get; set; }

		[JsonProperty("iden")]
		public string? Iden { get; set; }

		[JsonProperty("manufacturer")]
		public string? Manufacturer { get; set; }

		[JsonProperty("model")]
		public string? Model { get; set; }

		[JsonProperty("modified")]
		public float Modified { get; set; }

		[JsonProperty("nickname")]
		public string? Nickname { get; set; }

		[JsonProperty("push_token")]
		public string? PushToken { get; set; }
	}
}
