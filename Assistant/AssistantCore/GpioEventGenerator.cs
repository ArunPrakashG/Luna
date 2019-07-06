using HomeAssistant.Extensions;
using HomeAssistant.Log;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;

namespace HomeAssistant.AssistantCore {
	public sealed class GpioPinEventData {
		public int GpioPin { get; set; } = 2;
		public GpioPinDriveMode PinMode { get; set; } = GpioPinDriveMode.Output;
		public Enums.GpioPinEventStates PinEventState { get; set; } = Enums.GpioPinEventStates.ALL;
	}

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

	public sealed class GpioEventGenerator {
		private GPIOController Controller { get; set; }
		private Logger Logger { get; set; }
		private GpioEventManager Manager { get; set; }
		private bool OverrideEventWatcher { get; set; }
		public GpioPinEventData EventData { get; private set; }
		public (int, Thread) PollingThreadInfo { get; private set; }

		public (object sender, GpioPinValueChangedEventArgs e) GPIOEventPinValueStorage { get; private set; }
		public delegate void GPIOPinValueChangedEventHandler(object sender, GpioPinValueChangedEventArgs e);
		public event GPIOPinValueChangedEventHandler GPIOPinValueChanged;

		private (object sender, GpioPinValueChangedEventArgs e) GPIOPinValue {
			get => GPIOEventPinValueStorage;
			set {
				GPIOPinValueChanged?.Invoke(value.sender, value.e);
				GPIOEventPinValueStorage = GPIOPinValue;
			}
		}

		public GpioEventGenerator(GPIOController controller, GpioEventManager manager) {
			Controller = controller ?? throw new ArgumentNullException();
			Manager = manager ?? throw new ArgumentNullException();
			Logger = Manager.Logger;
		}

		public void OverridePinPolling() => OverrideEventWatcher = true;

		public void StartPinPolling(GpioPinEventData pinData) {
			if (pinData.GpioPin > 40 || pinData.GpioPin <= 0) {
				Logger.Log($"Specified pin is either > 40 or <= 0. Aborted. ({pinData.GpioPin})", Enums.LogLevels.Warn);
				return;
			}

			if (!Core.CoreInitiationCompleted) {
				return;
			}

			EventData = pinData;
			IGpioPin GPIOPin = Pi.Gpio[pinData.GpioPin];
			if (!Controller.SetGPIO(pinData.GpioPin, pinData.PinMode, GpioPinValue.High)) {
				Logger.Log($"Failed to set the pin status, cannot continue with the event for pin > {pinData.GpioPin}", Enums.LogLevels.Warn);
				return;
			}

			bool val = GPIOPin.Read();

			GPIOEventPinValueStorage = (this, new GpioPinValueChangedEventArgs() {
				Pin = GPIOPin,
				PinNumber = pinData.GpioPin,
				PinCurrentDigitalValue = val,
				PinPreviousDigitalValue = true,
				BcmPin = GPIOPin.BcmPin,
				GpioPhysicalPinNumber = GPIOPin.PhysicalPinNumber,
				IGpioPinValue = GPIOPin.Value,
				PinDriveMode = GPIOPin.PinMode,
				PinState = val ? Enums.GpioPinEventStates.OFF : Enums.GpioPinEventStates.ON,
				PinPreviousState = Enums.GpioPinEventStates.OFF
			});

			Logger.Log($"Started pin polling for {pinData.GpioPin}.", Core.Config.Debug ? Enums.LogLevels.Info : Enums.LogLevels.Trace);
			Enums.GpioPinEventStates previousValue = val ? Enums.GpioPinEventStates.OFF : Enums.GpioPinEventStates.ON;

			PollingThreadInfo = Helpers.InBackgroundThread(async () => {
				while (true) {
					if (OverrideEventWatcher) {
						return;
					}

					bool pinValue = GPIOPin.Read();

					//true = off
					//false = on
					Enums.GpioPinEventStates currentValue = pinValue ? Enums.GpioPinEventStates.OFF : Enums.GpioPinEventStates.ON;

					switch (pinData.PinEventState) {
						case Enums.GpioPinEventStates.OFF when currentValue == Enums.GpioPinEventStates.OFF:
							if (previousValue != currentValue) {
								GPIOPinValue = (this, new GpioPinValueChangedEventArgs() {
									Pin = GPIOPin,
									PinNumber = pinData.GpioPin,
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
									PinNumber = pinData.GpioPin,
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
									PinNumber = pinData.GpioPin,
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

			}, $"Polling thread {pinData.GpioPin}", true);
		}
	}

	public class GpioEventManager {
		internal readonly Logger Logger = new Logger("GPIO-EVENTS");
		public GPIOController Controller;
		public List<GpioEventGenerator> GpioPinEventGenerators = new List<GpioEventGenerator>();

		public GpioEventManager(GPIOController controller) {
			Controller = controller ?? throw new ArgumentNullException();
		}

		public void RegisterGpioEvent(GpioPinEventData pinData) {
			if (pinData == null) {
				return;
			}

			if (pinData.GpioPin > 40 || pinData.GpioPin <= 0) {
				Logger.Log($"Specified pin is either > 40 or <= 0. Aborted. ({pinData.GpioPin})", Enums.LogLevels.Warn);
				return;
			}

			if (!Core.CoreInitiationCompleted || Core.DisablePiMethods) {
				return;
			}

			GpioEventGenerator Generator = new GpioEventGenerator(Controller, this);
			Generator.StartPinPolling(pinData);
			GpioPinEventGenerators.Add(Generator);
		}

		public void RegisterGpioEvent(List<GpioPinEventData> pinDataList) {
			if (pinDataList == null || pinDataList.Count <= 0) {
				return;
			}

			foreach (GpioPinEventData pin in pinDataList) {
				if (pin.GpioPin > 40 || pin.GpioPin <= 0) {
					pinDataList.Remove(pin);
					Logger.Log($"Specified pin is either > 40 or <= 0. Removed from the list. ({pin.GpioPin})", Enums.LogLevels.Warn);
				}
			}

			if (!Core.CoreInitiationCompleted || Core.DisablePiMethods) {
				return;
			}

			foreach (GpioPinEventData pin in pinDataList) {
				GpioEventGenerator Generator = new GpioEventGenerator(Controller, this);
				Generator.StartPinPolling(pin);
				GpioPinEventGenerators.Add(Generator);
			}
		}

		public void ExitEventGenerator () {
			if (GpioPinEventGenerators == null || GpioPinEventGenerators.Count <= 0) {
				return;
			}

			foreach (GpioEventGenerator gen in GpioPinEventGenerators) {
				gen.OverridePinPolling();
				Logger.Log($"Stopping pin polling for {gen.EventData.GpioPin}", Enums.LogLevels.Trace);
			}
		}

		public void ExitEventGenerator (int pin) {
			if (GpioPinEventGenerators == null || GpioPinEventGenerators.Count <= 0) {
				return;
			}

			if (pin > 40 || pin <= 0) {
				return;
			}

			foreach (GpioEventGenerator gen in GpioPinEventGenerators) {
				if (gen.EventData.GpioPin.Equals(pin)) {
					gen.OverridePinPolling();
					Logger.Log($"Stopping pin polling for {gen.EventData.GpioPin}", Enums.LogLevels.Trace);
				}
			}
		}
	}
}
