using Assistant.Gpio.Controllers;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System.Collections.Generic;

namespace Assistant.Gpio.Events {
	public class GpioEventManager {
		internal readonly ILogger Logger = new Logger(typeof(GpioEventManager).Name);
		public List<GpioEventGenerator> GpioPinEventGenerators = new List<GpioEventGenerator>();
		private readonly PiController PiController;
		private readonly GpioPinController PinController;

		public GpioEventManager(PiController piController, GpioPinController pinController) {
			PiController = piController;
			PinController = pinController;
		}

		public bool RegisterGpioEvent(GpioPinEventConfig pinData) {
			if (pinData == null) {
				return false;
			}

			if (!PiController.IsValidPin(pinData.GpioPin)) {
				Logger.Warning("The specified pin is invalid.");
				return false;
			}

			GpioEventGenerator Generator = new GpioEventGenerator(PiController, PinController, this).InitEventGenerator();
			if (Generator.StartPinPolling(pinData)) {
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
				if (!PiController.IsValidPin(pin.GpioPin)) {
					Logger.Warning("Invalid pin in the pin list.");
					return false;
				}
			}

			foreach (GpioPinEventConfig pin in pinDataList) {
				RegisterGpioEvent(pin);
			}

			return true;
		}

		public void ExitEventGenerator() {
			if (GpioPinEventGenerators == null || GpioPinEventGenerators.Count <= 0) {
				return;
			}

			foreach (GpioEventGenerator gen in GpioPinEventGenerators) {
				ExitEventGenerator(gen.EventPinConfig.GpioPin);
			}
		}

		public void ExitEventGenerator(int pin) {
			if (GpioPinEventGenerators == null || GpioPinEventGenerators.Count <= 0) {
				return;
			}

			if (!PiController.IsValidPin(pin)) {
				return;
			}

			foreach (GpioEventGenerator gen in GpioPinEventGenerators) {
				if (gen.EventPinConfig.GpioPin == pin) {
					gen.OverridePinPolling();
					Logger.Trace($"Stopping pin polling for {gen.EventPinConfig.GpioPin} ...");
				}
			}
		}
	}
}
