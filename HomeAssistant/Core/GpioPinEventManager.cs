using HomeAssistant.Extensions;
using HomeAssistant.Log;
using System;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;

namespace HomeAssistant.Core {
	public class GpioPinValueChangedEventArgs {
		public IGpioPin Pin { get; set; }
		public int PinNumber { get; set; }
		public Enums.GpioPinEventStates PinState { get; set; }
		public Enums.GpioPinEventStates PinPreviousState { get; set; }
		public bool PinCurrentDigitalValue { get; set; }
		public bool PinPreviousDigitalValue { get; set; }
		public bool IGpioPinValue { get; set; }
		public BcmPin BcmPin { get; set; }
		public GpioPinDriveMode PinDriveMode { get; set; }
		public int GpioPhysicalPinNumber { get; set; }
	}

	public sealed class GpioPinEventManager {
		private GPIOController Controller { get; set; }
		private Logger Logger = new Logger("GPIO-EVENTS");
		private bool OverrideEventWatcher { get; set; }
		public (int, Thread) PollingThreadData { get; private set; }

		public (object sender, GpioPinValueChangedEventArgs e) GPIOEventPinValueStorage { get; private set; }

		private (object sender, GpioPinValueChangedEventArgs e) GPIOPinValue {
			get => GPIOEventPinValueStorage;
			set {
				GPIOPinValueChanged?.Invoke(value.sender, value.e);
				GPIOEventPinValueStorage = GPIOPinValue;
			}
		}

		public delegate void GPIOPinValueChangedEventHandler(object sender, GpioPinValueChangedEventArgs e);
		public event GPIOPinValueChangedEventHandler GPIOPinValueChanged;

		public GpioPinEventManager(GPIOController controller) => Controller = controller ?? throw new ArgumentNullException();

		public void OverrideEvents () => OverrideEventWatcher = true;

		public void StartPinPolling(int pin, GpioPinDriveMode mode = GpioPinDriveMode.Output, Enums.GpioPinEventStates registerValue = Enums.GpioPinEventStates.ALL) {
			if (pin > 40 || pin <= 0) {
				Logger.Log($"Specified pin is either > 40 or <= 0. Aborted. ({pin})", Enums.LogLevels.Warn);
				return;
			}

			if (!Tess.CoreInitiationCompleted) {
				return;
			}

			IGpioPin GPIOPin = Pi.Gpio[pin];
			Controller.SetGPIO(pin, mode, GpioPinValue.High);
			bool val = GPIOPin.Read();

			GPIOEventPinValueStorage = (this, new GpioPinValueChangedEventArgs() {
				Pin = GPIOPin,
				PinNumber = pin,
				PinCurrentDigitalValue = val,
				PinPreviousDigitalValue = true,
				BcmPin = GPIOPin.BcmPin,
				GpioPhysicalPinNumber = GPIOPin.PhysicalPinNumber,
				IGpioPinValue = GPIOPin.Value,
				PinDriveMode = GPIOPin.PinMode,
				PinState = val ? Enums.GpioPinEventStates.OFF : Enums.GpioPinEventStates.ON,
				PinPreviousState = Enums.GpioPinEventStates.OFF
			});

			Logger.Log($"Started pin polling for {pin}.", Tess.Config.Debug ? Enums.LogLevels.Info : Enums.LogLevels.Trace);
			Enums.GpioPinEventStates previousValue = val ? Enums.GpioPinEventStates.OFF : Enums.GpioPinEventStates.ON;

			PollingThreadData = Helpers.InBackgroundThread(async () => {
				while (true) {
					if (OverrideEventWatcher) {
						return;
					}

					bool pinValue = GPIOPin.Read();

					//Logger.Log($"pin = {pin} | Pin current value = {pinValue.ToString()} | pin previous value = {GPIOEventPinValueStorage.Item2.PinPreviousDigitalValue.ToString()}");

					//true = off
					//false = on
					Enums.GpioPinEventStates currentValue = pinValue ? Enums.GpioPinEventStates.OFF : Enums.GpioPinEventStates.ON;

					switch (registerValue) {
						case Enums.GpioPinEventStates.OFF when currentValue == Enums.GpioPinEventStates.OFF:
							if (previousValue != currentValue) {
								GPIOPinValue = (this, new GpioPinValueChangedEventArgs() {
									Pin = GPIOPin,
									PinNumber = pin,
									PinCurrentDigitalValue = pinValue,
									PinPreviousDigitalValue = GPIOEventPinValueStorage.e.PinCurrentDigitalValue,
									BcmPin = GPIOPin.BcmPin,
									GpioPhysicalPinNumber = GPIOPin.PhysicalPinNumber,
									IGpioPinValue = GPIOPin.Value,
									PinDriveMode = GPIOPin.PinMode,
									PinState = pinValue ? Enums.GpioPinEventStates.OFF : Enums.GpioPinEventStates.ON,
									PinPreviousState = previousValue
								});
							}
							break;
						case Enums.GpioPinEventStates.ON when currentValue == Enums.GpioPinEventStates.ON:
							if (previousValue != currentValue) {
								GPIOPinValue = (this, new GpioPinValueChangedEventArgs() {
									Pin = GPIOPin,
									PinNumber = pin,
									PinCurrentDigitalValue = pinValue,
									PinPreviousDigitalValue = GPIOEventPinValueStorage.e.PinCurrentDigitalValue,
									BcmPin = GPIOPin.BcmPin,
									GpioPhysicalPinNumber = GPIOPin.PhysicalPinNumber,
									IGpioPinValue = GPIOPin.Value,
									PinDriveMode = GPIOPin.PinMode,
									PinState = pinValue ? Enums.GpioPinEventStates.OFF : Enums.GpioPinEventStates.ON,
									PinPreviousState = previousValue
								});
							}
							break;
						case Enums.GpioPinEventStates.ALL:
							if (previousValue != currentValue) {
								GPIOPinValue = (this, new GpioPinValueChangedEventArgs() {
									Pin = GPIOPin,
									PinNumber = pin,
									PinCurrentDigitalValue = pinValue,
									PinPreviousDigitalValue = GPIOEventPinValueStorage.e.PinCurrentDigitalValue,
									BcmPin = GPIOPin.BcmPin,
									GpioPhysicalPinNumber = GPIOPin.PhysicalPinNumber,
									IGpioPinValue = GPIOPin.Value,
									PinDriveMode = GPIOPin.PinMode,
									PinState = pinValue ? Enums.GpioPinEventStates.OFF : Enums.GpioPinEventStates.ON,
									PinPreviousState = previousValue
								});
							}
							break;
						case Enums.GpioPinEventStates.NONE:
							break;
						default:
							break;
					}

					previousValue = currentValue;
					await Task.Delay(1).ConfigureAwait(false);
				}

			}, $"Polling thread {pin}", true);
		}
	}
}
