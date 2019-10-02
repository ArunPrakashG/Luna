using Assistant.Log;
using System.Collections.Generic;

namespace Assistant.AssistantCore.PiGpio {
	public class GpioEventManager {
		internal readonly Logger Logger = new Logger("GPIO-EVENTS");
		public List<GpioEventGenerator> GpioPinEventGenerators = new List<GpioEventGenerator>();

		public bool RegisterGpioEvent(GpioPinEventConfig pinData) {
			if (pinData == null) {
				return false;
			}

			if (!PiController.IsValidPin(pinData.GpioPin)) {
				Logger.Log("The specified pin is invalid.", Enums.LogLevels.Warn);
				return false;
			}

			GpioEventGenerator Generator = new GpioEventGenerator(this).InitEventGenerator();
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
					Logger.Log("Invalid pin in the pin list.", Enums.LogLevels.Warn);
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
					Logger.Log($"Stopping pin polling for {gen.EventPinConfig.GpioPin} ...", Enums.LogLevels.Trace);
				}
			}
		}
	}
}
