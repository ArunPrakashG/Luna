using Assistant.AssistantCore;
using Assistant.AssistantCore.PiGpio;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Assistant.Server.TCPServer.Responses {
	public class GetPins {
		[JsonProperty]
		public List<GpioPinConfig> RelayPinConfigs = new List<GpioPinConfig>();

		[JsonProperty]
		public List<GpioPinConfig> IrSensorConfigs = new List<GpioPinConfig>();

		[JsonProperty]
		public List<GpioPinConfig> SoundSensorConfigs = new List<GpioPinConfig>();

		[JsonProperty]
		public List<int> InputModePins = new List<int>();

		[JsonProperty]
		public List<int> OutputModePins = new List<int>();

		public string? GetJson() {
			if (Core.DisablePiMethods || Core.PiController == null || !Core.PiController.IsControllerProperlyInitialized) {
				return null;
			}

			if(Core.Config.RelayPins.Length > 0) {
				foreach (int pin in Core.Config.RelayPins) {
					GpioPinConfig config = Core.PiController.GetPinController().GetGpioConfig(pin);
					RelayPinConfigs.Add(config);
				}
			}			

			if(Core.Config.IRSensorPins.Length > 0) {
				foreach (int pin in Core.Config.IRSensorPins) {
					GpioPinConfig config = Core.PiController.GetPinController().GetGpioConfig(pin);
					IrSensorConfigs.Add(config);
				}
			}

			if(Core.Config.SoundSensorPins.Length > 0) {
				foreach (int pin in Core.Config.SoundSensorPins) {
					GpioPinConfig config = Core.PiController.GetPinController().GetGpioConfig(pin);
					SoundSensorConfigs.Add(config);
				}
			}

			if(Core.Config.InputModePins.Length > 0) {
				InputModePins = Core.Config.InputModePins.ToList();
			}

			if(Core.Config.OutputModePins.Length > 0) {
				OutputModePins = Core.Config.OutputModePins.ToList();
			}

			return JsonConvert.SerializeObject(this);
		}
	}
}
