namespace Assistant.AssistantCore {

	public static class Enums {

		public enum GpioPinEventStates : byte {
			ON,
			OFF,
			ALL,
			NONE
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

		public enum EGpioDriver {
			RaspberryIODriver,
			GpioDevicesDriver,
			WiringPiDriver
		}

		public enum AsyncModuleContext {
			AssistantStartup,
			AssistantShutdown,
			UpdateAvailable,
			UpdateStarted,
			NetworkDisconnected,
			NetworkReconnected,
			SystemShutdown,
			SystemRestart,
			ConfigWatcherEvent,
			ModuleWatcherEvent
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
			AssistantStartup,
			AssistantShutdown,
			NewEmaiNotification,
			Custom
		}

		public enum ModuleType {
			Discord,
			Email,
			Steam,
			Youtube,
			Events,
			Unknown
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

		public enum PiAudioState {
			Mute,
			Unmute
		}

		public enum GpioCycles : byte {
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
			Success
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
