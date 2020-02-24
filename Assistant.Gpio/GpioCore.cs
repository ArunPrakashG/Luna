using Assistant.Extensions.Interfaces;
using Assistant.Gpio.Controllers;
using Assistant.Gpio.Events;
using Assistant.Logging;
using static Assistant.Gpio.Controllers.PiController;

namespace Assistant.Gpio {
	public class GpioCore : IExternal {
		public readonly PiController? GpioController;
		public readonly EGPIO_DRIVERS GpioDriver = EGPIO_DRIVERS.RaspberryIODriver;
		private static AvailablePins OccupiedPins { get; set; }
		private readonly bool IsGracefullShutdownRequested = true;

		public GpioEventManager? GpioPollingManager { get; internal set; }
		public GpioMorseTranslator? MorseTranslator { get; internal set; }
		public BluetoothController? PiBluetooth { get; internal set; }
		public PiSoundController? PiSound { get; internal set; }
		public GpioPinController? PinController { get; internal set; }

		public GpioCore(EGPIO_DRIVERS driver, bool gracefulExit) {
			GpioController = new PiController(this);
			GpioDriver = driver;

			IsGracefullShutdownRequested = gracefulExit;
		}

		public GpioCore? InitGpioCore(AvailablePins pins) {
			if (GpioController == null || !IsAllowedToExecute) {
				return null;
			}

			OccupiedPins = pins;
			PiController? result = GpioController.InitController(IsGracefullShutdownRequested);

			if (result != null && result.IsControllerProperlyInitialized) {
				return this;
			}

			return null;
		}

		internal static AvailablePins GetOccupiedPins() => OccupiedPins;

		public void RegisterLoggerEvent(object eventHandler) => LoggerExtensions.RegisterLoggerEvent(eventHandler);
	}
}
