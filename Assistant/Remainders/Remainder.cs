using Newtonsoft.Json;
using System;

namespace Assistant.Remainders {
	public class Remainder {
		[JsonProperty]
		public string? UniqueId { get; set; }

		[JsonProperty]
		public string? Message { get; set; }

		[JsonProperty]
		public DateTime RemaindAt { get; set; }

		[JsonProperty]
		public bool IsCompleted { get; set; }
	}
}
