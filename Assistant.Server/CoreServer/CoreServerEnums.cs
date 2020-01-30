namespace Assistant.Server.CoreServer {
	public class CoreServerEnums {
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

		public enum GPIO_EVENT_STATES : byte {
			ON,
			OFF,
			ALL,
			NONE
		}

		public enum GPIO_PIN_MODE : int {
			Input = 0,
			Output = 1,
			Alt01 = 4,
			Alt02 = 5
		}

		public enum GPIO_PIN_STATE : int {
			On = 0,
			Off = 1
		}
	}
}
