using Assistant.Gpio.Events.EventArgs;
using Assistant.Logging.Interfaces;

namespace Assistant.Gpio {
	internal class SensorEvents {
		private readonly ILogger Logger;

		internal SensorEvents(ILogger _logger) => Logger = _logger;

		internal bool IrSensorEvent(OnValueChangedEventArgs e) {
			Logger.Info($"IR Sensor | '{e.Pin}' -> '{e.CurrentState}' | ({e.PreviousPinState})");
			return true;
		}

		internal bool RelaySwitchEvent(OnValueChangedEventArgs e) {
			Logger.Info($"Relay | '{e.Pin}' -> '{e.CurrentState}' | ({e.PreviousPinState})");
			return true;
		}

		internal bool SoundSensorEvent(OnValueChangedEventArgs e) {
			Logger.Info($"Sound Sensor | '{e.Pin}' -> '{e.CurrentState}' | ({e.PreviousPinState})");
			return true;
		}
	}
}
