
//    _  _  ___  __  __ ___     _   ___ ___ ___ ___ _____ _   _  _ _____
//   | || |/ _ \|  \/  | __|   /_\ / __/ __|_ _/ __|_   _/_\ | \| |_   _|
//   | __ | (_) | |\/| | _|   / _ \\__ \__ \| |\__ \ | |/ _ \| .` | | |
//   |_||_|\___/|_|  |_|___| /_/ \_\___/___/___|___/ |_/_/ \_\_|\_| |_|
//

//MIT License

//Copyright(c) 2019 Arun Prakash
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

namespace Assistant.AssistantCore {

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
			SteamClients,
			YoutubeClients,
			AllModules,
			None,
			CustomModules
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
			CONNECTION_FAIL = 0x08,
			EXECUTION_FAIL = 0x09
		}
	}
}
