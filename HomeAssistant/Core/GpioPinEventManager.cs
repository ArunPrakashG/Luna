using HomeAssistant.Extensions;
using HomeAssistant.Log;
using System;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;

namespace HomeAssistant.Core {
	public class GpioPinEventManager {

		private GPIOController Controller { get; set; }
		private Logger Logger = new Logger("GPIO-EVENTS");
		public (int, Thread) PollingThreadData { get; private set; }

		private (int pin, bool pinValue) GPIOPinValue {
			get => GPIOPinValue;
			set {
				GPIOPinValueChanged?.Invoke(value.pin, value.pinValue, GPIOPinValue.pinValue);
			}
		}

		public delegate void GPIOPinValueChangedEventHandler(int pin, bool currentValue, bool previousValue);
		public event GPIOPinValueChangedEventHandler GPIOPinValueChanged;

		public GpioPinEventManager(GPIOController controller) => Controller = controller ?? throw new ArgumentNullException();

		//true = off
		//false = on
		public void StartPinPolling(int pin, GpioPinDriveMode mode = GpioPinDriveMode.Output) {
			if (pin > 40 || pin <= 0) {
				Logger.Log($"Specified pin is either > 40 or <= 0. Aborted. ({pin})", Enums.LogLevels.Warn);
				return;
			}

			IGpioPin GPIOPin = Pi.Gpio[pin];
			GPIOPin.PinMode = mode;

			Logger.Log($"Started pin polling for {pin}.", Tess.Config.Debug ? Enums.LogLevels.Info : Enums.LogLevels.Trace);

			PollingThreadData = Helpers.InBackgroundThread(async () => {
				while (true) {
					GPIOPinValue = (pin, GPIOPin.Read());
					await Task.Delay(20).ConfigureAwait(false);
				}
			}, "Polling thread", true);
		}
	}
}
