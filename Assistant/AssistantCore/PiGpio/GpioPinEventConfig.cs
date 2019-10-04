using static Assistant.AssistantCore.Enums;

namespace Assistant.AssistantCore.PiGpio {
	public sealed class GpioPinEventConfig {
		public int GpioPin { get; set; } = 2;

		public GpioPinMode PinMode { get; set; } = GpioPinMode.Input;

		public GpioPinEventStates PinEventState { get; set; } = GpioPinEventStates.ALL;

		public GpioPinEventConfig(int _gpioPin, GpioPinMode _pinMode, GpioPinEventStates _pinEventState) {
			GpioPin = _gpioPin;
			PinMode = _pinMode;
			PinEventState = _pinEventState;
		}

		public GpioPinEventConfig() {

		}
	}
}
