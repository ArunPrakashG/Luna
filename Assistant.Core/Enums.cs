namespace Assistant.Core {

	public static class Enums {

		public enum SteamUserInputType {
			DeviceID,
			Login,
			Password,
			SteamGuard,
			SteamParentalPIN,
			TwoFactorAuthentication,
			Unknown
		}

		public enum SteamPermissionLevels {
			Owner,
			Master,
			Operator
		}

		public enum SpeechContext : byte {
			AssistantStartup,
			AssistantShutdown,
			NewEmaiNotification,
			Custom
		}

		public enum KeywordType {
			Subject,
			MessageBody,
			From
		}

		//TODO Global Error code system
		public enum ServerErrors : byte {
			AUTH_FAIL = 0x01,
			CONTEXT_FAIL = 0x02,
			STATE_FAIL = 0x03,
			VOLTAGE_FAIL = 0x04,
			GPIO_FAIL = 0x05,
			STATUS_FAIL = 0x06,
			PIN_FAIL = 0x07,
			CONNECTION_FAIL = 0x08,
			EXECUTION_FAIL = 0x09
		}
	}
}
