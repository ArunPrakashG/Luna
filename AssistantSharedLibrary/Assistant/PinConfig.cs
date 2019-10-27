using Newtonsoft.Json;
using System.Text;
using static AssistantSharedLibrary.Assistant.Enums;

namespace AssistantSharedLibrary.Assistant {
	public class PinConfig {
		[JsonProperty]
		public int Pin { get; set; } = 0;

		[JsonProperty]
		public GpioPinState PinValue { get; set; }

		[JsonProperty]
		public GpioPinMode Mode { get; set; }

		[JsonProperty]
		public bool IsDelayedTaskSet { get; set; }

		[JsonProperty]
		public int TaskSetAfterMinutes { get; set; }

		[JsonProperty]
		public bool IsPinOn => PinValue == GpioPinState.On;

		public override bool Equals(object obj) {
			if (obj == null) {
				return false;
			}

			PinConfig config = (PinConfig) obj;
			return config.Pin == Pin;
		}

		public override int GetHashCode() => base.GetHashCode();

		public static string AsJson(PinConfig config) {
			if (config == null) {
				return string.Empty;
			}

			return JsonConvert.SerializeObject(config);
		}

		public override string ToString() {
			StringBuilder s = new StringBuilder();
			s.AppendLine("---------------------------");
			s.AppendLine($"Pin -> {Pin}");
			s.AppendLine($"Pin Value -> {PinValue.ToString()}");
			s.AppendLine($"Pin Mode -> {Mode.ToString()}");
			s.AppendLine($"Is Tasked -> {IsDelayedTaskSet}");
			s.AppendLine($"Task set after minutes -> {TaskSetAfterMinutes}");
			s.AppendLine($"Is pin on -> {IsPinOn}");
			s.AppendLine("---------------------------");
			return s.ToString();
		}
	}
}
