using Assistant.Gpio.Controllers;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assistant.Gpio.Events {
	public class EventManager {
		private static readonly ILogger Logger = new Logger(typeof(EventManager).Name);
		private static readonly Dictionary<int, Generator> Events = new Dictionary<int, Generator>();

		public bool RegisterEvent(EventConfig config) {
			if (!IOController.IsValidPin(config.GpioPin)) {
				Logger.Warning("The specified pin is invalid.");
				return false;
			}

			if (Events.ContainsKey(config.GpioPin)) {
				return false;
			}

			Generator gen = new Generator(config, Logger);

			for (int i = 0; i < 5; i++) {
				if (gen.Config.IsEventRegistered) {
					break;
				}

				Task.Delay(30).Wait();
			}

			if (!gen.Config.IsEventRegistered) {
				return false;
			}

			Events.Add(config.GpioPin, gen);
			return gen.Config.IsEventRegistered;
		}

		public void StopAllEventGenerators() {
			for(int i = 0; i < Events.Count; i++) {
				Events[i].OverrideGeneration();
				Logger.Trace($"Stopped pin polling for '{Events[i].Config.GpioPin}' pin");
			}
		}

		public void StopEventGeneratorForPin(int pin) {
			if (!IOController.IsValidPin(pin)) {
				return;
			}

			for (int i = 0; i < Events.Count; i++) {
				if(Events[i].Config.GpioPin != pin) {
					continue;
				}

				Events[i].OverrideGeneration();
				Logger.Trace($"Stopped pin polling for '{Events[i].Config.GpioPin}' pin");
			}
		}
	}
}
