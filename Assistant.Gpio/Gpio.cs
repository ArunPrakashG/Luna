using Assistant.Extensions.Interfaces;
using Assistant.Logging;
using static Assistant.Gpio.PiController;

namespace Assistant.Gpio {
	public class Gpio : IExternal {
		public readonly PiController? GpioController;
		public readonly EGPIO_DRIVERS GpioDriver = EGPIO_DRIVERS.RaspberryIODriver;
		private static (int[] InputPins, int[] OutputPins) OccupiedPins { get; set; }
		private readonly bool IsGracefullShutdownRequested = true;

		public GpioEventManager? GpioPollingManager { get; internal set; }
		public GpioMorseTranslator? MorseTranslator { get; internal set; }
		public BluetoothController? PiBluetooth { get; internal set; }
		public SoundController? PiSound { get; internal set; }
		public GpioPinController? PinController { get; internal set; }

		public Gpio(EGPIO_DRIVERS driver, bool gracefulExit) {
			GpioController = new PiController(this);
			GpioDriver = driver;
			
			IsGracefullShutdownRequested = gracefulExit;
		}

		public Gpio? InitGpioCore(int[] outputPins, int[] inputPins) {
			if(GpioController == null || !IsAllowedToExecute) {
				return null;
			}

			OccupiedPins = (outputPins, inputPins);
			PiController? result = GpioController.InitController(IsGracefullShutdownRequested);

			if(result != null && result.IsControllerProperlyInitialized) {
				return this;
			}

			return null;
		}

		internal static (int[] output, int[] input) GetOccupiedPins() => OccupiedPins;

		public void RegisterLoggerEvent(object eventHandler) => LoggerExtensions.RegisterLoggerEvent(eventHandler);
	}
}
