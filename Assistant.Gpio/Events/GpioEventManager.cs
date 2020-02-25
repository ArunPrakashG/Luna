using Assistant.Gpio.Controllers;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System.Collections.Generic;

namespace Assistant.Gpio.Events {
	public class GpioEventManager {
		internal static readonly ILogger Logger = new Logger(typeof(GpioEventManager).Name);
		public List<GpioEventGenerator> GpioPinEventGenerators = new List<GpioEventGenerator>();

		public bool RegisterGpioEvent(GpioPinEventConfig pinConfig) {
			if (!PinController.IsValidPin(pinConfig.GpioPin)) {
				Logger.Warning("The specified pin is invalid.");
				return false;
			}

			GpioEventGenerator Generator = new GpioEventGenerator();

			if (Generator.StartPinPolling(pinConfig)) {
				GpioPinEventGenerators.Add(Generator);
				return true;
			}

			return false;
		}

		public bool RegisterGpioEvent(List<GpioPinEventConfig> pinDataList) {
			if (pinDataList == null || pinDataList.Count <= 0) {
				return false;
			}

			foreach (GpioPinEventConfig pin in pinDataList) {
				if (!PinController.IsValidPin(pin.GpioPin)) {
					Logger.Warning("Invalid pin in the pin list.");
					return false;
				}
			}

			foreach (GpioPinEventConfig pin in pinDataList) {
				RegisterGpioEvent(pin);
			}

			return true;
		}

		public void StopAllEventGenerators() {
			if (GpioPinEventGenerators == null || GpioPinEventGenerators.Count <= 0) {
				return;
			}

			foreach (GpioEventGenerator gen in GpioPinEventGenerators) {
				StopEventGeneratorForPin(gen.EventPinConfig.GpioPin);
			}
		}

		public void StopEventGeneratorForPin(int pin) {
			if (GpioPinEventGenerators == null || GpioPinEventGenerators.Count <= 0) {
				return;
			}

			if (!PinController.IsValidPin(pin)) {
				return;
			}

			foreach (GpioEventGenerator gen in GpioPinEventGenerators) {
				if (gen.EventPinConfig.GpioPin == pin) {
					gen.OverridePinPolling();
					Logger.Trace($"Stopped pin polling for '{gen.EventPinConfig.GpioPin}' pin");
				}
			}
		}
	}
}
