using Newtonsoft.Json;
using System;

namespace Assistant.Remainders {
	public class Remainder {
		[JsonProperty]
		public string UniqueId { get; set; } = string.Empty;

		[JsonProperty]
		public string Message { get; set; } = string.Empty;

		[JsonProperty]
		public DateTime RemaindAt { get; set; }

		[JsonProperty]
		public bool IsCompleted { get; set; }
	}
}
