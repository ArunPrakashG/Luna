namespace HomeAssistant.Core {

	public static class Enums {

		public enum GpioPinEventStates : byte {
			ON,
			OFF,
			ALL,
			NONE
		}

		public enum ModuleLoaderContext : byte {
			EmailClients,
			DiscordClients,
			GoogleMaps,
			MiscModules,
			SteamClients,
			YoutubeClients,
			Logger,
			All,
			None
		}

		public enum PiContext : byte {
			GPIO,
			RESTART,
			SHUTDOWN,
			FETCH,
			RELAY
		}

		public enum SteamUserInputType {
			DeviceID,
			Login,
			Password,
			SteamGuard,
			SteamParentalPIN,
			TwoFactorAuthentication,
			Unknown
		}

		public enum ModulesContext {
			Email,
			Discord,
			GoogleMap,
			Steam,
			Youtube,
			Logger,
			Misc,
			Default
		}

		public enum SteamPermissionLevels {
			Owner,
			Master,
			Operator
		}

		public enum HttpStatusCodes {
			Accepted = 202,
			Ambiguous = 300,
			BadGateway = 502,
			BadRequest = 400,
			Conflict = 409,
			Continue = 100,
			Created = 201,
			ExceptionFailed = 417,
			Forbidden = 403,
			Found = 302,
			GatewayTimeout = 504,
			Gone = 410,
			HttpVersionNotSupported = 505,
			InternalServerError = 500,
			LengthRequired = 411,
			MethodNotAllowed = 405,
			Moved = 301,
			MovedPermanently = 301,
			MultipleChoices = 300,
			NoContent = 204,
			NonAuthoritativeInformation = 203,
			NotAcceptable = 406,
			NotFound = 404,
			NotImplemented = 501,
			NotModified = 304,
			OK = 200,
			Redirect = 302,
			RequestTimeout = 408,
			ServiceUnavailable = 503,
			Unauthorized = 401
		}
		
		public enum SpeechContext : byte {
			TessStartup,
			TessShutdown,
			NewEmaiNotification,
			Custom
		}

		public enum KeywordType {
			Subject,
			MessageBody,
			From
		}

		public enum NotificationContext : byte {
			Imap,
			EmailSend,
			EmailSendFailed,
			FatalError,
			Normal
		}

		public enum PinMode {
			Output = 0,
			Input = 1
		}

		public enum PiAudioState {
			Mute,
			Unmute
		}

		public enum GPIOCycles : byte {
			Cycle,
			Single,
			Base,
			OneMany,
			OneTwo,
			OneOne,
			Default
		}

		public enum LogLevels {
			Trace,
			Debug,
			Info,
			Warn,
			Error,
			Fatal,
			DebugException,
			Ascii,
			UserInput,
			ServerResult,
			Custom,
			Sucess
		}

		public enum PiVoltage : byte {
			HIGH,
			LOW
		}

		public enum PiPinNumber {
			PIN_1,
			PIN_2,
			PIN_3,
			PIN_4,
			PIN_5,
			PIN_6,
			PIN_7,
			PIN_8,
			PIN_9,
			PIN_10,
			PIN_11,
			PIN_12,
			PIN_13,
			PIN_14,
			PIN_15,
			PIN_16,
			PIN_17,
			PIN_18,
			PIN_19,
			PIN_20,
			PIN_21,
			PIN_22,
			PIN_23,
			PIN_24,
			PIN_25,
			PIN_26,
			PIN_27,
			PIN_28,
			PIN_29,
			PIN_30,
			PIN_31
		}

		public enum PiState {
			INPUT = 0,
			OUTPUT = 1
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
			CONNECTION_FAIL = 0x08
		}
	}
}
