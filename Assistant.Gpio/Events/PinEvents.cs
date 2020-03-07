using Assistant.Gpio.Controllers;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assistant.Gpio.Events {
	public class PinEvents {
		internal static readonly ILogger Logger = new Logger(typeof(PinEvents).Name);
		internal static readonly Dictionary<int, Generator> Events = new Dictionary<int, Generator>();

		public bool RegisterEvent(EventConfig config) {
			if (!PinController.IsValidPin(config.GpioPin)) {
				Logger.Warning("The specified pin is invalid.");
				return false;
			}

			Generator gen = new Generator(config);
			gen.Poll();

			for (int i = 0; i < 5; i++) {
				if (gen.IsEventRegistered) {
					break;
				}

				Task.Delay(30).Wait();
			}

			if (!gen.IsEventRegistered) {
				return false;
			}

			Events.Add(config.GpioPin, gen);
			return gen.IsEventRegistered;
		}

		public void StopAllEventGenerators() {
			if (Events == null || Events.Count <= 0) {
				return;
			}

			for(int i = 0; i < Events.Count; i++) {
				Events[i].OverridePolling();
				Logger.Trace($"Stopped pin polling for '{Events[i].Config.GpioPin}' pin");
			}
		}

		public void StopEventGeneratorForPin(int pin) {
			if (Events == null || Events.Count <= 0) {
				return;
			}

			if (!PinController.IsValidPin(pin)) {
				return;
			}

			for (int i = 0; i < Events.Count; i++) {
				if (Events[i].Config.GpioPin == pin) {
					Events[i].OverridePolling();
					Logger.Trace($"Stopped pin polling for '{Events[i].Config.GpioPin}' pin");
				}
			}
		}
	}
}
