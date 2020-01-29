namespace Assistant.Servers.TCPServer {
	public class CommandEnums {
		public enum CommandType {
			InvalidType = 0,
			Gpio = 1,
			Remainder = 2,
			Alarm = 3,
			Weather = 4,
			Client = 5
		}

		public enum ResponseObjectType {
			SetGpio,
			GetGpioAll,
			SetGpioDelayed,
			SetRemainder,
			SetAlarm,
			GetGpio,
			AssistantInfo,
			PiInfo,
			GetWeather,
			Invalid,
			NoResponse
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
			GetGpioAll = 9,
			GetAssistantInfo = 10,
			GetPiInfo = 11,
			ShutdownAssistant = 12,
			ShutdownPi = 13,
			RestartAssistant = 14
		}

		public enum CommandResponseCode {
			OK,
			FAIL,
			INVALID,
			FATAL
		}
	}
}
