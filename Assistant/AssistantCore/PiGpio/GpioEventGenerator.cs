
//    _  _  ___  __  __ ___     _   ___ ___ ___ ___ _____ _   _  _ _____
//   | || |/ _ \|  \/  | __|   /_\ / __/ __|_ _/ __|_   _/_\ | \| |_   _|
//   | __ | (_) | |\/| | _|   / _ \\__ \__ \| |\__ \ | |/ _ \| .` | | |
//   |_||_|\___/|_|  |_|___| /_/ \_\___/___/___|___/ |_/_/ \_\_|\_| |_|
//

//MIT License

//Copyright(c) 2019 Arun Prakash
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using Assistant.Extensions;
using Assistant.Log;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.WiringPi;

namespace Assistant.AssistantCore.PiGpio {

	public sealed class GpioPinEventConfig {

		public int GpioPin { get; set; } = 2;

		public GpioPinDriveMode PinMode { get; set; } = GpioPinDriveMode.Output;

		public Enums.GpioPinEventStates PinEventState { get; set; } = Enums.GpioPinEventStates.ALL;
	}

	public class GpioPinValueChangedEventArgs {

		public GpioPin Pin { get; set; }

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

		private PiController Controller { get; set; }

		private Logger Logger { get; set; }

		private GpioEventManager EventManager { get; set; }

		private bool OverrideEventWatcher { get; set; }

		public GpioPinEventConfig EventPinConfig { get; private set; }
		public bool IsEventRegistered { get; private set; } = false;

		public (int, Thread) PollingThreadInfo { get; private set; }

		public (object sender, GpioPinValueChangedEventArgs e) _GpioPinValueChanged { get; private set; }

		public delegate void GPIOPinValueChangedEventHandler(object sender, GpioPinValueChangedEventArgs e);

		public event GPIOPinValueChangedEventHandler GpioPinValueChanged;

		private (object sender, GpioPinValueChangedEventArgs e) GPIOPinValue {
			get => _GpioPinValueChanged;
			set {
				GpioPinValueChanged?.Invoke(value.sender, value.e);
				_GpioPinValueChanged = GPIOPinValue;
			}
		}

		public GpioEventGenerator(PiController controller, GpioEventManager manager) {
			Controller = controller ?? throw new ArgumentNullException();
			EventManager = manager ?? throw new ArgumentNullException();
			Logger = EventManager.Logger;
		}

		public void OverridePinPolling() => OverrideEventWatcher = true;

		private async Task StartInputPollingAsync() {
			if(!Core.CoreInitiationCompleted) {
				return;
			}

			if (IsEventRegistered) {
				Logger.Log("There already seems to have an event registered on this instance.", Enums.LogLevels.Warn);
				return;
			}

			if (EventPinConfig.GpioPin > 40 || EventPinConfig.GpioPin <= 0) {
				Logger.Log($"Specified pin is either > 40 or <= 0. Aborted. ({EventPinConfig.GpioPin})", Enums.LogLevels.Warn);
				return;
			}

			if(EventPinConfig.PinMode != GpioPinDriveMode.Input) {
				Logger.Log($"Internal error.", Enums.LogLevels.Warn);
				return;
			}

			GpioPin pin = (GpioPin) Pi.Gpio[EventPinConfig.GpioPin];
			//let the method in controller to set it so that we can later add config system for pins and we can access config from that method for saving etc
			//pin.PinMode = GpioPinDriveMode.Input;
			if(!Controller.SetGpioValue(EventPinConfig.GpioPin, GpioPinDriveMode.Input)) {
				Logger.Log($"Failed to set the pin status, cannot continue with the event for pin > {EventPinConfig.GpioPin}", Enums.LogLevels.Warn);
				return;
			}

			//true = off
			//false = on
			bool initialValue = await pin.ReadAsync().ConfigureAwait(false);
			Enums.GpioPinEventStates previousValue = initialValue ? Enums.GpioPinEventStates.OFF : Enums.GpioPinEventStates.ON;

			_GpioPinValueChanged = (this, new GpioPinValueChangedEventArgs() {
				Pin = pin,
				PinNumber = EventPinConfig.GpioPin,
				PinCurrentDigitalValue = initialValue,
				PinPreviousDigitalValue = true,
				BcmPin = pin.BcmPin,
				GpioPhysicalPinNumber = pin.PhysicalPinNumber,
				IGpioPinValue = pin.Value,
				PinDriveMode = pin.PinMode,
				PinState = previousValue,
				PinPreviousState = Enums.GpioPinEventStates.OFF
			});

			Logger.Log($"Started input pin polling for {EventPinConfig.GpioPin}.", Enums.LogLevels.Trace);
			IsEventRegistered = true;

			PollingThreadInfo = Helpers.InBackgroundThread(async () => {
				while (!OverrideEventWatcher) {
					bool pinValue = await pin.ReadAsync().ConfigureAwait(false);
					Enums.GpioPinEventStates currentValue = pinValue ? Enums.GpioPinEventStates.OFF : Enums.GpioPinEventStates.ON;

					switch (EventPinConfig.PinEventState) {
						case Enums.GpioPinEventStates.OFF when currentValue == Enums.GpioPinEventStates.OFF:
							if (previousValue == currentValue) {
								break;
							}

							GPIOPinValue = (this, new GpioPinValueChangedEventArgs() {
								Pin = pin,
								PinNumber = EventPinConfig.GpioPin,
								PinCurrentDigitalValue = pinValue,
								PinPreviousDigitalValue = _GpioPinValueChanged.e.PinCurrentDigitalValue,
								BcmPin = pin.BcmPin,
								GpioPhysicalPinNumber = pin.PhysicalPinNumber,
								IGpioPinValue = pin.Value,
								PinDriveMode = pin.PinMode,
								PinState = pinValue ? Enums.GpioPinEventStates.OFF : Enums.GpioPinEventStates.ON,
								PinPreviousState = previousValue
							});
							break;

						case Enums.GpioPinEventStates.ON when currentValue == Enums.GpioPinEventStates.ON:
							if (previousValue == currentValue) {
								break;
							}

							GPIOPinValue = (this, new GpioPinValueChangedEventArgs() {
								Pin = pin,
								PinNumber = EventPinConfig.GpioPin,
								PinCurrentDigitalValue = pinValue,
								PinPreviousDigitalValue = _GpioPinValueChanged.e.PinCurrentDigitalValue,
								BcmPin = pin.BcmPin,
								GpioPhysicalPinNumber = pin.PhysicalPinNumber,
								IGpioPinValue = pin.Value,
								PinDriveMode = pin.PinMode,
								PinState = pinValue ? Enums.GpioPinEventStates.OFF : Enums.GpioPinEventStates.ON,
								PinPreviousState = previousValue
							});
							break;

						case Enums.GpioPinEventStates.ALL:
							if (previousValue == currentValue) {
								break;
							}

							GPIOPinValue = (this, new GpioPinValueChangedEventArgs() {
								Pin = pin,
								PinNumber = EventPinConfig.GpioPin,
								PinCurrentDigitalValue = pinValue,
								PinPreviousDigitalValue = _GpioPinValueChanged.e.PinCurrentDigitalValue,
								BcmPin = pin.BcmPin,
								GpioPhysicalPinNumber = pin.PhysicalPinNumber,
								IGpioPinValue = pin.Value,
								PinDriveMode = pin.PinMode,
								PinState = pinValue ? Enums.GpioPinEventStates.OFF : Enums.GpioPinEventStates.ON,
								PinPreviousState = previousValue
							});
							break;

						case Enums.GpioPinEventStates.NONE:
							break;

						default:
							break;
					}

					previousValue = currentValue;
					await Task.Delay(1).ConfigureAwait(false);
				}
			}, $"Polling thread {EventPinConfig.GpioPin}", true);
		}

		private async Task StartOutputPollingAsync() {
			if (!Core.CoreInitiationCompleted) {
				return;
			}

			if (IsEventRegistered) {
				Logger.Log("There already seems to have an event registered on this instance.", Enums.LogLevels.Warn);
				return;
			}

			if (EventPinConfig.GpioPin > 40 || EventPinConfig.GpioPin <= 0) {
				Logger.Log($"Specified pin is either > 40 or <= 0. Aborted. ({EventPinConfig.GpioPin})", Enums.LogLevels.Warn);
				return;
			}

			if (EventPinConfig.PinMode != GpioPinDriveMode.Output) {
				Logger.Log($"Internal error.", Enums.LogLevels.Warn);
				return;
			}

			GpioPin pin = (GpioPin) Pi.Gpio[EventPinConfig.GpioPin];
			//let the method in controller to set it so that we can later add config system for pins and we can access config from that method for saving etc
			//pin.PinMode = GpioPinDriveMode.Output;
			if (!Controller.SetGpioValue(EventPinConfig.GpioPin, GpioPinDriveMode.Output, GpioPinValue.High)) {
				Logger.Log($"Failed to set the pin status, cannot continue with the event for pin > {EventPinConfig.GpioPin}", Enums.LogLevels.Warn);
				return;
			}

			//true = off
			//false = on
			bool initialValue = await pin.ReadAsync().ConfigureAwait(false);
			Enums.GpioPinEventStates previousValue = initialValue ? Enums.GpioPinEventStates.OFF : Enums.GpioPinEventStates.ON;

			_GpioPinValueChanged = (this, new GpioPinValueChangedEventArgs() {
				Pin = pin,
				PinNumber = EventPinConfig.GpioPin,
				PinCurrentDigitalValue = initialValue,
				PinPreviousDigitalValue = true,
				BcmPin = pin.BcmPin,
				GpioPhysicalPinNumber = pin.PhysicalPinNumber,
				IGpioPinValue = pin.Value,
				PinDriveMode = pin.PinMode,
				PinState = previousValue,
				PinPreviousState = Enums.GpioPinEventStates.OFF
			});

			Logger.Log($"Started output pin polling for {EventPinConfig.GpioPin}.", Enums.LogLevels.Trace);
			IsEventRegistered = true;

			PollingThreadInfo = Helpers.InBackgroundThread(async () => {
				while (!OverrideEventWatcher) {
					bool pinValue = await pin.ReadAsync().ConfigureAwait(false);
					Enums.GpioPinEventStates currentValue = pinValue ? Enums.GpioPinEventStates.OFF : Enums.GpioPinEventStates.ON;

					switch (EventPinConfig.PinEventState) {
						case Enums.GpioPinEventStates.OFF when currentValue == Enums.GpioPinEventStates.OFF:
							if (previousValue == currentValue) {
								break;
							}

							GPIOPinValue = (this, new GpioPinValueChangedEventArgs() {
								Pin = pin,
								PinNumber = EventPinConfig.GpioPin,
								PinCurrentDigitalValue = pinValue,
								PinPreviousDigitalValue = _GpioPinValueChanged.e.PinCurrentDigitalValue,
								BcmPin = pin.BcmPin,
								GpioPhysicalPinNumber = pin.PhysicalPinNumber,
								IGpioPinValue = pin.Value,
								PinDriveMode = pin.PinMode,
								PinState = pinValue ? Enums.GpioPinEventStates.OFF : Enums.GpioPinEventStates.ON,
								PinPreviousState = previousValue
							});
							break;

						case Enums.GpioPinEventStates.ON when currentValue == Enums.GpioPinEventStates.ON:
							if (previousValue == currentValue) {
								break;
							}

							GPIOPinValue = (this, new GpioPinValueChangedEventArgs() {
								Pin = pin,
								PinNumber = EventPinConfig.GpioPin,
								PinCurrentDigitalValue = pinValue,
								PinPreviousDigitalValue = _GpioPinValueChanged.e.PinCurrentDigitalValue,
								BcmPin = pin.BcmPin,
								GpioPhysicalPinNumber = pin.PhysicalPinNumber,
								IGpioPinValue = pin.Value,
								PinDriveMode = pin.PinMode,
								PinState = pinValue ? Enums.GpioPinEventStates.OFF : Enums.GpioPinEventStates.ON,
								PinPreviousState = previousValue
							});
							break;

						case Enums.GpioPinEventStates.ALL:
							if (previousValue == currentValue) {
								break;
							}

							GPIOPinValue = (this, new GpioPinValueChangedEventArgs() {
								Pin = pin,
								PinNumber = EventPinConfig.GpioPin,
								PinCurrentDigitalValue = pinValue,
								PinPreviousDigitalValue = _GpioPinValueChanged.e.PinCurrentDigitalValue,
								BcmPin = pin.BcmPin,
								GpioPhysicalPinNumber = pin.PhysicalPinNumber,
								IGpioPinValue = pin.Value,
								PinDriveMode = pin.PinMode,
								PinState = pinValue ? Enums.GpioPinEventStates.OFF : Enums.GpioPinEventStates.ON,
								PinPreviousState = previousValue
							});
							break;

						case Enums.GpioPinEventStates.NONE:
							break;

						default:
							break;
					}

					previousValue = currentValue;
					await Task.Delay(1).ConfigureAwait(false);
				}
			}, $"Polling thread {EventPinConfig.GpioPin}", true);
		}

		public async Task<bool> StartPinPolling(GpioPinEventConfig config) {
			if (config.GpioPin > 40 || config.GpioPin <= 0) {
				Logger.Log($"Specified pin is either > 40 or <= 0. Aborted. ({config.GpioPin})", Enums.LogLevels.Warn);
				return false;
			}

			if (!Core.CoreInitiationCompleted) {
				return false;
			}

			EventPinConfig = config;
			if(config.PinMode == GpioPinDriveMode.Output) {
				await StartOutputPollingAsync().ConfigureAwait(false);
				return true;
			}
			else if(config.PinMode == GpioPinDriveMode.Input) {
				await StartInputPollingAsync().ConfigureAwait(false);
				return true;
			}
			else {
				Logger.Log("Modes other than Output/Input currently isn't supported. (yet)", Enums.LogLevels.Warn);
				return false;
			}			
		}
	}

	public class GpioEventManager {
		internal readonly Logger Logger = new Logger("GPIO-EVENTS");
		private PiController Controller { get; set; }
		public List<GpioEventGenerator> GpioPinEventGenerators = new List<GpioEventGenerator>();

		public GpioEventManager(PiController controller) {
			Controller = controller ?? throw new ArgumentNullException();
		}

		public bool RegisterGpioEvent(GpioPinEventConfig pinData) {
			if (pinData == null) {
				return false;
			}

			if (pinData.GpioPin > 40 || pinData.GpioPin <= 0) {
				Logger.Log($"Specified pin is either > 40 or <= 0. Aborted. ({pinData.GpioPin})", Enums.LogLevels.Warn);
				return false;
			}

			if (!Core.CoreInitiationCompleted || Core.DisablePiMethods) {
				return false;
			}

			GpioEventGenerator Generator = new GpioEventGenerator(Controller, this);
			if (Generator.StartPinPolling(pinData).Result) {
				GpioPinEventGenerators.Add(Generator);
				return true;
			}

			return false;
		}

		public void RegisterGpioEvent(List<GpioPinEventConfig> pinDataList) {
			if (pinDataList == null || pinDataList.Count <= 0) {
				return;
			}

			foreach (GpioPinEventConfig pin in pinDataList) {
				if (pin.GpioPin > 40 || pin.GpioPin <= 0) {
					pinDataList.Remove(pin);
					Logger.Log($"Specified pin is either > 40 or <= 0. Removed from the list. ({pin.GpioPin})", Enums.LogLevels.Warn);
				}
			}

			if (!Core.CoreInitiationCompleted || Core.DisablePiMethods) {
				return;
			}

			foreach (GpioPinEventConfig pin in pinDataList) {
				GpioEventGenerator Generator = new GpioEventGenerator(Controller, this);
				if (Generator.StartPinPolling(pin).Result) {
					GpioPinEventGenerators.Add(Generator);
				}				
			}
		}

		public void ExitEventGenerator() {
			if (GpioPinEventGenerators == null || GpioPinEventGenerators.Count <= 0) {
				return;
			}

			foreach (GpioEventGenerator gen in GpioPinEventGenerators) {
				gen.OverridePinPolling();
				Logger.Log($"Stopping pin polling for {gen.EventPinConfig.GpioPin}", Enums.LogLevels.Trace);
			}
		}

		public void ExitEventGenerator(int pin) {
			if (GpioPinEventGenerators == null || GpioPinEventGenerators.Count <= 0) {
				return;
			}

			if (pin > 40 || pin <= 0) {
				return;
			}

			foreach (GpioEventGenerator gen in GpioPinEventGenerators) {
				if (gen.EventPinConfig.GpioPin.Equals(pin)) {
					gen.OverridePinPolling();
					Logger.Log($"Stopping pin polling for {gen.EventPinConfig.GpioPin}", Enums.LogLevels.Trace);
				}
			}
		}
	}
}
