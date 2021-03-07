using Luna.Gpio.Drivers;
using Luna.Gpio.Exceptions;
using Luna.Logging;
using System;
using System.Linq;
using Unosquare.RaspberryIO;
using static Luna.Gpio.Enums;

namespace Luna.Gpio.Controllers {
	internal class PinController {
		private readonly InternalLogger Logger = new InternalLogger(nameof(PinController));
		private static GpioControllerDriver CurrentDriver;
		private static bool IsAlreadyInit;
		private readonly GpioCore Controller;

		internal PinController(GpioCore core) => Controller = core;

		internal void InitPinController(GpioControllerDriver driver) {
			if (!GpioCore.IsAllowedToExecute || IsAlreadyInit) {
				return;
			}

			CurrentDriver = driver.Init() ?? throw new DriverInitializationFailedException(nameof(driver));
			IsAlreadyInit = true;
		}

		internal static GpioControllerDriver GetDriver() => GpioCore.IsAllowedToExecute && CurrentDriver.IsDriverInitialized && IsAlreadyInit ? CurrentDriver : throw new DriverNotInitializedException();

		internal static bool IsValidPin(int pin) {
			if (!GpioCore.IsAllowedToExecute || !Constants.BcmGpioPins.Contains(pin)) {
				return false;
			}

			GpioControllerDriver driver = GetDriver() ?? throw new DriverNotInitializedException();

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
