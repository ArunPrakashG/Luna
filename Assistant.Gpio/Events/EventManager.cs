using Assistant.Gpio.Controllers;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assistant.Gpio.Events {
	public class EventManager {
		private readonly ILogger Logger = new Logger(typeof(EventManager).Name);
		private readonly Dictionary<int, Generator> Events = new Dictionary<int, Generator>();
		private readonly GpioController Controller;

		internal EventManager(GpioController _controller) => Controller = _controller;

		internal async Task<bool> RegisterEvent(EventConfig config) {
			if (!PinController.IsValidPin(config.GpioPin)) {
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

				await Task.Delay(30).ConfigureAwait(false);
			}

			if (!gen.Config.IsEventRegistered) {
				return false;
			}

			Events.Add(config.GpioPin, gen);
			return gen.Config.IsEventRegistered;
		}

		internal void StopAllEventGenerators() {
			for(int i = 0; i < Events.Count; i++) {
				Events[i].OverrideGeneration();
				Logger.Trace($"Stopped pin polling for '{Events[i].Config.GpioPin}' pin");
			}
		}

		internal void StopEventGeneratorForPin(int pin) {
			if (!PinController.IsValidPin(pin)) {
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
