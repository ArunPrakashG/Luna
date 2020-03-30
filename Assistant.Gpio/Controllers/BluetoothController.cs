using Assistant.Gpio.Drivers;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using static Assistant.Logging.Enums;

namespace Assistant.Gpio.Controllers {

	/// <summary>
	/// This class is only allowed to be used if we have the Generic driver (RaspberryIO driver)
	/// </summary>
	public class BluetoothController {
		private readonly ILogger Logger = new Logger(typeof(BluetoothController).Name);
		private IGpioControllerDriver? Driver => IOController.GetDriver();
		public bool IsBluetoothControllerInitialized { get; private set; }

		public BluetoothController InitBluetoothController() {
			if (Driver == null || !GpioController.IsAllowedToExecute) {
				IsBluetoothControllerInitialized = false;
				return this;
			}

			if (!Driver.IsDriverProperlyInitialized || Driver.DriverName != Enums.EGPIO_DRIVERS.RaspberryIODriver) {
				IsBluetoothControllerInitialized = false;
				return this;
			}

			IsBluetoothControllerInitialized = true;
			return this;
		}

		public async Task<bool> FetchControllers() {
			if (!IsBluetoothControllerInitialized) {
				return false;
			}

			Logger.Log("Fetching blue-tooth controllers...");

			foreach (string i in await Pi.Bluetooth.ListControllers().ConfigureAwait(false)) {
				Logger.Log($"FOUND > {i}");
			}

			Logger.Log("Finished fetching controllers.");
			return true;
		}

		public async Task<bool> FetchDevices() {
			if (!IsBluetoothControllerInitialized) {
				return false;
			}

			Logger.Log("Fetching blue-tooth devices...");

			foreach (string dev in await Pi.Bluetooth.ListDevices().ConfigureAwait(false)) {
				Logger.Log($"FOUND > {dev}");
			}

			Logger.Log("Finished fetching devices.");
			return true;
		}

		public async Task<bool> TurnOnBluetooth() {
			if (!IsBluetoothControllerInitialized) {
				return false;
			}

			if (await Pi.Bluetooth.PowerOn().ConfigureAwait(false)) {
				Logger.Log("Blue-tooth has been turned on.");
				return true;
			}

			Logger.Log("Failed to turn on blue-tooth.", LogLevels.Warn);
			return false;
		}

		public async Task<bool> TurnOffBluetooth() {
			if (!IsBluetoothControllerInitialized) {
				return false;
			}

			if (await Pi.Bluetooth.PowerOff().ConfigureAwait(false)) {
				Logger.Log("Blue-tooth has been turned off.");
				return true;
			}

			Logger.Log("Failed to turn off blue-tooth.", LogLevels.Warn);
			return false;
		}
	}
}
