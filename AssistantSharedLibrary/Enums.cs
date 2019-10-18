namespace AssistantSharedLibrary {
	public class Enums {
		public enum CommandType {
			InvalidType = 0,
			Gpio = 1,
			Remainder = 2,
			Alarm = 3,
			Weather = 4,
			Client = 5
		}

		public enum Command {
			InvalidCommand = 0,
			SetGpioGeneral = 1,
			SetGpioDelayed = 2,
			GetGpio = 3,
			GetWeather = 4,
			SetRemainder = 5,
			SetAlarm = 6,
			Disconnect = 7,
			Initiate = 8,
			GetOutputPins = 9,
			GetInputPins = 10
		}

		public enum CommandResponseCode {
			OK,
			FAIL,
			INVALID,
			FATAL
		}

		public enum GpioPinMode {
			Input = 0,
			Output = 1,
			Alt01 = 4,
			Alt02 = 5
		}

		public enum GpioPinState {
			On = 0,
			Off = 1
		}
	}
}
