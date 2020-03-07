using Assistant.Extensions;
using Assistant.Gpio.Drivers;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System;
using System.Linq;
using Unosquare.RaspberryIO;

namespace Assistant.Gpio.Controllers {
	public class PinController {
		internal static readonly ILogger Logger = new Logger(typeof(PinController).Name);
		private static IGpioControllerDriver CurrentDriver;
		private static bool IsAlreadyInit;

		public void InitPinController<T>(T driver, Enums.NumberingScheme numberingScheme = Enums.NumberingScheme.Logical) where T : IGpioControllerDriver {
			if (!PiGpioController.IsAllowedToExecute || IsAlreadyInit) {
				return;
			}

			CurrentDriver = driver.InitDriver(numberingScheme);
			IsAlreadyInit = true;
		}

		public static IGpioControllerDriver? GetDriver() {
			if (CurrentDriver == null) {
				Logger.Warning("Driver has malfunctioned/hasn't been initialized yet.");
				return null;
			}

			if (!PiGpioController.IsAllowedToExecute || !CurrentDriver.IsDriverProperlyInitialized) {
				Logger.Warning("Incorrect OS/Driver not initialized; Please initialize correctly.");
				return null;
			}

			if (!IsAlreadyInit) {
				Logger.Warning("Pin controller isn't initialized yet.");
				return null;
			}

			return CurrentDriver;
		}

		public static bool IsValidPin(int pin) {
			if (!PiGpioController.IsAllowedToExecute || !Constants.BcmGpioPins.Contains(pin)) {
				return false;
			}

			try {
				if (!Pi.Gpio.Contains(Pi.Gpio[pin])) {
					Logger.Warning($"pin {pin} doesn't exist or is not a valid Bcm Gpio pin.");
					return false;
				}
			}
			catch (Exception e) {
				Logger.Trace(e.ToString());
				return false;
			}

			return true;
		}
	}
}
