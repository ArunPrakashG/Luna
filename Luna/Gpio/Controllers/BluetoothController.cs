using Luna.Logging;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;

namespace Luna.Gpio.Controllers {
	/// <summary>
	/// This class is only allowed to be used if we have the Generic driver (RaspberryIO driver)
	/// </summary>
	public class BluetoothController {
		private readonly InternalLogger Logger = new InternalLogger(nameof(BluetoothController));		
		private readonly GpioCore Controller;

		private bool IsPossible
			=> PinController.GetDriver() != null && GpioCore.IsAllowedToExecute && PinController.GetDriver()?.DriverName == Enums.GpioDriver.RaspberryIODriver;

		internal BluetoothController(GpioCore _controller) => Controller = _controller;

		internal async Task<bool> FetchControllers() {
			if (!IsPossible) {
				return false;
			}

			Logger.Info("Fetching bluetooth controllers...");

			foreach (string i in await Pi.Bluetooth.ListControllers().ConfigureAwait(false)) {
				Logger.Info($"FOUND > {i}");
			}

			Logger.Info("Finished fetching controllers.");
			return true;
		}

		internal async Task<bool> FetchDevices() {
			if (!IsPossible) {
				return false;
			}

			Logger.Info("Fetching bluetooth devices...");

			foreach (string dev in await Pi.Bluetooth.ListDevices().ConfigureAwait(false)) {
				Logger.Info($"FOUND > {dev}");
			}

			Logger.Info("Finished fetching devices.");
			return true;
		}

		internal async Task<bool> TurnOnBluetooth() {
			if (!IsPossible) {
				return false;
			}

			if (await Pi.Bluetooth.PowerOn().ConfigureAwait(false)) {
				Logger.Info("Bluetooth has been turned on.");
				return true;
			}

			Logger.Warn("Failed to turn on bluetooth.");
			return false;
		}

		internal async Task<bool> TurnOffBluetooth() {
			if (!IsPossible) {
				return false;
			}

			if (await Pi.Bluetooth.PowerOff().ConfigureAwait(false)) {
				Logger.Info("Bluetooth has been turned off.");
				return true;
			}

			Logger.Warn("Failed to turn off bluetooth.");
			return false;
		}
	}
}
