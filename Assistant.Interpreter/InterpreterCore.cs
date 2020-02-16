using Assistant.Extensions.Interfaces;
using Assistant.Logging;
using Assistant.Logging.Interfaces;

namespace Assistant.Interpreter {
	public class InterpreterCore : IExternal {
		internal static readonly ILogger Logger = new Logger("INTERPRETER-CORE");

		public InterpreterCore() { }

		public enum COMMAND_CODE : byte {
			HELP_BASIC = 0x00,
			HELP_ADVANCED = 0x01,
			HELP_ALL = 0x02,
			RELAY_BASIC = 0x03,
			RELAY_DELAYED_TASK = 0x04,
			DEVICE_SHUTDOWN = 0x05,
			DEVICE_REBOOT = 0x06,
			APP_EXIT = 0x07,
			APP_RESTART = 0x08,
			APP_UPDATE = 0x09,
			BASH_COMMAND = 0x10,
			BASH_SCRIPT_PATH = 0x11,
			GPIO_CYCLE_TEST = 0x12,
			GPIO_SHUTDOWN = 0x13,
			TTS_SPEAK = 0x14,
			PLAY_ALERT = 0x15,
			PLAY_ALARM = 0x16,
			SET_ALARM = 0x17,
			SET_REMAINDER = 0x18,
			INVALID = 0x99
		}

		public void RegisterLoggerEvent(object eventHandler) => LoggerExtensions.RegisterLoggerEvent(eventHandler);
	}
}
