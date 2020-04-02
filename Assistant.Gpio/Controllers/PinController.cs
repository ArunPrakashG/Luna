using Assistant.Extensions;
using Assistant.Gpio.Drivers;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System;
using System.Linq;
using Unosquare.RaspberryIO;

namespace Assistant.Gpio.Controllers {
	public class PinController {
		private static readonly ILogger Logger = new Logger(nameof(PinController));
		private static IGpioControllerDriver CurrentDriver;
		private static bool IsAlreadyInit;
		private readonly GpioController Controller;

		internal PinController(GpioController _controller) => Controller = _controller;

		internal void InitPinController<T>(T driver, Enums.NumberingScheme numberingScheme = Enums.NumberingScheme.Logical) where T : IGpioControllerDriver {
			if (!GpioController.IsAllowedToExecute || IsAlreadyInit) {
				return;
			}

			CurrentDriver = driver.InitDriver(numberingScheme);
			IsAlreadyInit = true;
		}

		public static IGpioControllerDriver? GetDriver() => GpioController.IsAllowedToExecute && CurrentDriver.IsDriverInitialized && IsAlreadyInit ? CurrentDriver : null;

		internal static ILogger GetLogger() => Logger;

		public static bool IsValidPin(int pin) {
			if (!GpioController.IsAllowedToExecute || !Constants.BcmGpioPins.Contains(pin)) {
				return false;
			}

			if (GetDriver()?.DriverName != Enums.GpioDriver.RaspberryIODriver) {
				return Constants.BcmGpioPins.Contains(pin);
			}

			if (!Pi.Gpio.Contains(Pi.Gpio[pin])) {
				return false;
			}

			return true;
		}
	}
}
