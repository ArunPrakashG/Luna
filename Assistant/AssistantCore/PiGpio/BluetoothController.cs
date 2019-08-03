using Assistant.Log;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;

namespace Assistant.AssistantCore.PiGpio {
	public class PiBluetoothController {

		private readonly Logger Logger = new Logger("PI-BLUETOOTH");

		private GPIOController Controller => Core.Controller;

		private async Task<bool> FetchControllers() {
			if (!Core.CoreInitiationCompleted) {
				return false;
			}

			if (Controller == null) {
				return false;
			}

			Logger.Log("Fetching bluetooth controllers...");
			foreach (string i in await Pi.Bluetooth.ListControllers().ConfigureAwait(false)) {
				Logger.Log($"FOUND > {i}");
			}
			Logger.Log("Finished fetching controllers.");
			return true;
		}

		private async Task<bool> FetchDevices() {
			if (!Core.CoreInitiationCompleted) {
				return false;
			}

			if (Controller == null) {
				return false;
			}

			Logger.Log("Fetching bluetooth devices...");
			foreach (string dev in await Pi.Bluetooth.ListDevices().ConfigureAwait(false)) {
				Logger.Log($"FOUND > {dev}");
			}
			Logger.Log("Finished fetching devices.");
			return true;
		}

		private async Task<bool> TurnOnBluetooth () {
			if (Controller == null) {
				return false;
			}

			if (await Pi.Bluetooth.PowerOn().ConfigureAwait(false)) {
				Logger.Log("Bluetooth has been turned on.");
				return true;
			}
			else {
				Logger.Log("Failed to turn on bluetooth.", Enums.LogLevels.Warn);
				return false;
			}
		}

		private async Task<bool> TurnOffBluetooth () {
			if (Controller == null) {
				return false;
			}

			if (await Pi.Bluetooth.PowerOff().ConfigureAwait(false)) {
				Logger.Log("Bluetooth has been turned off.");
				return true;
			}
			else {
				Logger.Log("Failed to turn off bluetooth.", Enums.LogLevels.Warn);
				return false;
			}
		}

	}
}
