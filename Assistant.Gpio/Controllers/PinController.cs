using Assistant.Extensions;
using Assistant.Gpio.Drivers;
using Assistant.Gpio.Exceptions;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System;
using System.Linq;
using Unosquare.RaspberryIO;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Controllers {
	public class PinController {
		private readonly ILogger Logger = new Logger(nameof(PinController));
		private static IGpioControllerDriver CurrentDriver;
		private static bool IsAlreadyInit;
		private readonly GpioCore Controller;

		internal PinController(GpioCore _controller) => Controller = _controller;

		internal void InitPinController(IGpioControllerDriver driver, NumberingScheme numberingScheme = NumberingScheme.Logical) {
			if (!GpioCore.IsAllowedToExecute || IsAlreadyInit) {
				return;
			}

			CurrentDriver = driver.InitDriver(Logger, Controller.GetAvailablePins(), numberingScheme) ?? throw new DriverInitializationFailedException(nameof(driver));
			IsAlreadyInit = true;
		}

		public static IGpioControllerDriver GetDriver() => GpioCore.IsAllowedToExecute && CurrentDriver.IsDriverInitialized && IsAlreadyInit ? CurrentDriver : throw new DriverNotInitializedException();
		
		public static bool IsValidPin(int pin) {
			if (!GpioCore.IsAllowedToExecute || !Constants.BcmGpioPins.Contains(pin)) {
				return false;
			}

			IGpioControllerDriver driver = GetDriver() ?? throw new DriverNotInitializedException();

			return driver.DriverName switch
			{
				Enums.GpioDriver.RaspberryIODriver => Pi.Gpio.Contains(Pi.Gpio[pin]),
				Enums.GpioDriver.SystemDevicesDriver => Constants.BcmGpioPins.Contains(pin),
				Enums.GpioDriver.WiringPiDriver => Constants.BcmGpioPins.Contains(pin),
				_ => true,
			};
		}
	}
}
