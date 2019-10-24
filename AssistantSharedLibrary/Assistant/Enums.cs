namespace AssistantSharedLibrary.Assistant {
	public class Enums {
		public enum TYPE_CODE : int {
			UNKNOWN,
			SET_ALARM,
			SET_GPIO,
			SET_GPIO_DELAYED,
			SET_REMAINDER,
			GET_GPIO,
			GET_GPIO_PIN,
			GET_WEATHER,
			GET_ASSISTANT_INFO,
			GET_PI_INFO,
			SET_PI,
			SET_ASSISTANT,
			EVENT_PIN_STATE
		}

		public enum RESPONSE_STATUS_CODE : byte {
			OK = 0x00,
			FAIL = 0x01,
			INVALID = 0x02,
			FATAL = 0x10
		}

		public enum GpioPinEventStates : byte {
			ON,
			OFF,
			ALL,
			NONE
		}

		public enum GpioPinMode : int {
			Input = 0,
			Output = 1,
			Alt01 = 4,
			Alt02 = 5
		}

		public enum GpioPinState : int {
			On = 0,
			Off = 1
		}

	}
}
