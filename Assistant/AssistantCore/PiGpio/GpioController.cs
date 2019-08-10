
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
using System.Devices.Gpio;
using System.Linq;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;

namespace Assistant.AssistantCore.PiGpio {

	//High = OFF
	//Low = ON
	public class GPIOController {
		private readonly Logger Logger = new Logger("GPIO-CONTROLLER");
		private GpioConfigHandler GPIOConfigHandler { get; set; }

		public List<GpioPinConfig> GpioConfigCollection { get; set; }

		public GpioConfigRoot GpioConfigRoot { get; private set; }

		public GpioEventManager GpioPollingManager { get; private set; }

		public GpioMorseTranslator MorseTranslator { get; private set; }

		public GPIOController(GpioConfigRoot rootObject, List<GpioPinConfig> config, GpioConfigHandler configHandler) {
			GpioConfigCollection = config;
			GPIOConfigHandler = configHandler;
			GpioConfigRoot = rootObject;
			GpioPollingManager = new GpioEventManager(this);
			MorseTranslator = new GpioMorseTranslator(this);

			Helpers.ScheduleTask(() => {
				if (GpioPollingManager == null) {
					return;
				}

				List<GpioPinEventData> pinData = new List<GpioPinEventData>();

				foreach (int pin in Core.Config.RelayPins) {
					pinData.Add(new GpioPinEventData() {
						PinMode = GpioPinDriveMode.Output,
						GpioPin = pin,
						PinEventState = Enums.GpioPinEventStates.ALL
					});
				}

				GpioPollingManager.RegisterGpioEvent(pinData);

				foreach (GpioEventGenerator c in GpioPollingManager.GpioPinEventGenerators) {
					c.GPIOPinValueChanged += OnGpioPinValueChanged;
				}
			}, TimeSpan.FromSeconds(5));


			Logger.Log("Initiated GPIO Controller class!", Enums.LogLevels.Trace);
		}

		[Obsolete("Testing purpose")]
		private void OnGpioPinValueChanged(object sender, GpioPinValueChangedEventArgs e) {
			switch (e.PinState) {
				case Enums.GpioPinEventStates.OFF when e.PinPreviousState == Enums.GpioPinEventStates.OFF:
					Logger.Log($"{e.PinNumber} gpio pin set to OFF state. (OFF)", Enums.LogLevels.Trace);
					break;

				case Enums.GpioPinEventStates.ON when e.PinPreviousState == Enums.GpioPinEventStates.ON:
					Logger.Log($"{e.PinNumber} gpio pin set to ON state. (ON)", Enums.LogLevels.Trace);
					break;

				case Enums.GpioPinEventStates.ON when e.PinPreviousState == Enums.GpioPinEventStates.OFF:
					Logger.Log($"{e.PinNumber} gpio pin set to ON state. (OFF)", Enums.LogLevels.Trace);
					break;

				case Enums.GpioPinEventStates.OFF when e.PinPreviousState == Enums.GpioPinEventStates.ON:
					Logger.Log($"{e.PinNumber} gpio pin set to OFF state. (ON)", Enums.LogLevels.Trace);
					break;

				default:
					Logger.Log($"Value for {e.PinNumber} pin changed to {e.PinCurrentDigitalValue} from {e.PinPreviousDigitalValue.ToString()}");
					break;
			}
		}

		[Obsolete("System.Devices.Gpio test method")]
		public void GpioTest() {
			System.Devices.Gpio.GpioController controllerTest = new System.Devices.Gpio.GpioController(PinNumberingScheme.Gpio);
			if (!controllerTest.IsPinOpen(Core.Config.RelayPins.FirstOrDefault())) {
				controllerTest.OpenPin(Core.Config.RelayPins.FirstOrDefault());
			}

			controllerTest[Core.Config.RelayPins.FirstOrDefault()].Mode = PinMode.Output;
			controllerTest[Core.Config.RelayPins.FirstOrDefault()].NotifyEvents = PinEvent.Any;
			controllerTest[Core.Config.RelayPins.FirstOrDefault()].EnableRaisingEvents = true;
			controllerTest[Core.Config.RelayPins.FirstOrDefault()].ValueChanged += OnPinValueChangedTest;
		}

		[Obsolete("System.Devices.Gpio test method")]
		private void OnPinValueChangedTest(object sender, PinValueChangedEventArgs e) {
			Logger.Log($"pin value changed test method fired for pin {e.GpioPinNumber}");
		}

		private bool CheckSafeMode() => Core.Config.GPIOSafeMode;

		public GpioPinConfig FetchPinStatus(int pin) {
			GpioPinConfig Status = new GpioPinConfig();

			foreach (GpioPinConfig value in GpioConfigCollection) {
				if (value.Pin.Equals(pin)) {
					Status.IsOn = value.IsOn;
					Status.Mode = value.Mode;
					Status.Pin = value.Pin;
					return Status;
				}
			}

			return Status;
		}

		public GpioPinConfig FetchPinStatus(BcmPin pin) {
			GpioPinConfig Status = new GpioPinConfig();

			foreach (GpioPinConfig value in GpioConfigCollection) {
				if (value.Pin.Equals(pin)) {
					Status.IsOn = value.IsOn;
					Status.Mode = value.Mode;
					Status.Pin = value.Pin;
					return Status;
				}
			}

			return Status;
		}

		public async Task<bool> SetGPIO(int pin, GpioPinDriveMode mode, GpioPinValue state, int timeoutDuration) {
			if (pin <= 0 || timeoutDuration <= 0) {
				return false;
			}

			if (SetGPIO(pin, mode, state)) {
				await Task.Delay(timeoutDuration).ConfigureAwait(false);
				return SetGPIO(pin, mode, GpioPinValue.High);
			}
			return false;
		}

		public bool GpioDigitalRead(int pin, bool onlyInputPins = false) {
			IGpioPin GPIOPin = Pi.Gpio[pin];

			if (onlyInputPins && GPIOPin.PinMode != GpioPinDriveMode.Input) {
				Logger.Log("The specified gpio pin mode isn't set to Input.", Enums.LogLevels.Error);
				return false;
			}

			return GPIOPin.Read();
		}

		public bool SetGPIO(int pin, GpioPinDriveMode mode, GpioPinValue state) {
			if (!Core.Config.EnableGpioControl) {
				return false;
			}

			if (CheckSafeMode()) {
				if (!Core.Config.RelayPins.Contains(pin)) {
					Logger.Log($"Could not configure {pin} as it's marked as SAFE. (SAFE-MODE)");
					return false;
				}
			}

			GpioPinConfig Status = FetchPinStatus(pin);

			switch (state) {
				case GpioPinValue.High:
					if (Status.Pin.Equals(pin) && !Status.IsOn) {
						return true;
					}
					break;

				case GpioPinValue.Low:
					if (Status.Pin.Equals(pin) && Status.IsOn) {
						return true;
					}
					break;

				default:
					return false;
			}

			IGpioPin GPIOPin = Pi.Gpio[pin];
			GPIOPin.PinMode = mode;
			GPIOPin.Write(state);

			switch (mode) {
				case GpioPinDriveMode.Input:
					switch (state) {
						case GpioPinValue.High:
							Logger.Log($"Configured {pin} pin to OFF. (INPUT)", Enums.LogLevels.Trace);
							UpdatePinStatus(pin, false, Enums.PinMode.Input);
							break;

						case GpioPinValue.Low:
							Logger.Log($"Configured {pin} pin to ON. (INPUT)", Enums.LogLevels.Trace);
							UpdatePinStatus(pin, true, Enums.PinMode.Input);
							break;
					}
					break;

				case GpioPinDriveMode.Output:
					switch (state) {
						case GpioPinValue.High:
							Logger.Log($"Configured {pin} pin to OFF. (OUTPUT)", Enums.LogLevels.Trace);
							UpdatePinStatus(pin, false);
							break;

						case GpioPinValue.Low:
							Logger.Log($"Configured {pin} pin to ON. (OUTPUT)", Enums.LogLevels.Trace);
							UpdatePinStatus(pin, true);
							break;
					}
					break;

				default:
					goto case GpioPinDriveMode.Output;
			}

			return true;
		}

		public bool SetGPIO(BcmPin pin, GpioPinDriveMode mode, GpioPinValue state) {
			if (!Core.Config.EnableGpioControl) {
				return false;
			}

			if (CheckSafeMode()) {
				if (!Core.Config.RelayPins.Contains((int) pin)) {
					Logger.Log("Could not configure {pin} as safe mode is enabled.");
					return false;
				}
			}

			GpioPinConfig Status = FetchPinStatus(pin);

			switch (state) {
				case GpioPinValue.High:
					if (Status.Pin.Equals(pin) && !Status.IsOn) {
						return true;
					}
					break;

				case GpioPinValue.Low:
					if (Status.Pin.Equals(pin) && Status.IsOn) {
						return true;
					}
					break;

				default:
					return false;
			}

			try {
				IGpioPin GPIOPin = Pi.Gpio[pin];
				GPIOPin.PinMode = mode;
				GPIOPin.Write(state);

				switch (mode) {
					case GpioPinDriveMode.Input:
						switch (state) {
							case GpioPinValue.High:
								Logger.Log($"Configured {pin.ToString()} pin to OFF. (INPUT)", Enums.LogLevels.Trace);
								UpdatePinStatus(pin, false, Enums.PinMode.Input);
								break;

							case GpioPinValue.Low:
								Logger.Log($"Configured {pin.ToString()} pin to ON. (INPUT)", Enums.LogLevels.Trace);
								UpdatePinStatus(pin, true, Enums.PinMode.Input);
								break;
						}
						break;

					case GpioPinDriveMode.Output:
						switch (state) {
							case GpioPinValue.High:
								Logger.Log($"Configured {pin.ToString()} pin to OFF. (OUTPUT)", Enums.LogLevels.Trace);
								UpdatePinStatus(pin, false);
								break;

							case GpioPinValue.Low:
								Logger.Log($"Configured {pin.ToString()} pin to ON. (OUTPUT)", Enums.LogLevels.Trace);
								UpdatePinStatus(pin, true);
								break;
						}
						break;

					default:
						goto case GpioPinDriveMode.Output;
				}

				return true;
			}
			catch (Exception e) {
				Logger.Log(e.ToString(), Enums.LogLevels.Error);
				return false;
			}
		}

		private void UpdatePinStatus(int pin, bool isOn, Enums.PinMode mode = Enums.PinMode.Output) {
			foreach (GpioPinConfig value in GpioConfigCollection) {
				if (value.Pin.Equals(pin)) {
					value.Pin = pin;
					value.IsOn = isOn;
					value.Mode = mode;
				}
			}
		}

		private void UpdatePinStatus(BcmPin pin, bool isOn, Enums.PinMode mode = Enums.PinMode.Output) {
			foreach (GpioPinConfig value in GpioConfigCollection) {
				if (value.Pin.Equals(pin)) {
					value.Pin = (int) pin;
					value.IsOn = isOn;
					value.Mode = mode;
				}
			}
		}

		public async Task InitShutdown() {
			if (!GpioConfigCollection.Any() || GpioConfigCollection == null) {
				return;
			}

			await Task.Delay(100).ConfigureAwait(false);

			if (GpioPollingManager != null) {
				foreach (GpioEventGenerator c in GpioPollingManager.GpioPinEventGenerators) {
					c.GPIOPinValueChanged -= OnGpioPinValueChanged;
				}
			}

			GpioPollingManager.ExitEventGenerator();

			if (Core.Config.CloseRelayOnShutdown) {
				foreach (GpioPinConfig value in GpioConfigCollection) {
					if (value.IsOn) {
						SetGPIO(value.Pin, GpioPinDriveMode.Output, GpioPinValue.High);
					}
				}
			}

			GpioConfigRoot.GPIOData = GpioConfigCollection;
			GPIOConfigHandler.SaveGPIOConfig(GpioConfigRoot);
		}

		public async Task<bool> RelayTestService(Enums.GPIOCycles selectedCycle, int singleChannelValue = 0) {
			Logger.Log("Relay test service started!");

			switch (selectedCycle) {
				case Enums.GPIOCycles.OneTwo:
					_ = await RelayOneTwo().ConfigureAwait(false);
					break;

				case Enums.GPIOCycles.OneOne:
					_ = await RelayOneOne().ConfigureAwait(false);
					break;

				case Enums.GPIOCycles.OneMany:
					_ = await RelayOneMany().ConfigureAwait(false);
					break;

				case Enums.GPIOCycles.Cycle:
					_ = await RelayOneTwo().ConfigureAwait(false);
					_ = await RelayOneOne().ConfigureAwait(false);
					_ = await RelayOneMany().ConfigureAwait(false);
					break;

				case Enums.GPIOCycles.Single:
					_ = await RelaySingle(singleChannelValue, 8000).ConfigureAwait(false);
					break;

				case Enums.GPIOCycles.Base:
					Logger.Log("Base argument specified, running default cycle test!");
					goto case Enums.GPIOCycles.Cycle;
				case Enums.GPIOCycles.Default:
					Logger.Log("Unknown value, Aborting...");
					break;
			}
			return true;
		}

		public async Task<bool> RelaySingle(int pin = 0, int delayInMs = 8000) {
			SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.Low);
			Logger.Log($"Waiting for {delayInMs} ms to close the relay...");
			await System.Threading.Tasks.Task.Delay(delayInMs).ConfigureAwait(false);
			SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.High);
			Logger.Log("Relay closed!");
			return true;
		}

		public async Task<bool> RelayOneTwo() {

			//make sure all relay is off
			foreach (int pin in Core.Config.RelayPins) {
				foreach (GpioPinConfig pinvalue in GpioConfigCollection) {
					if (pin.Equals(pinvalue.Pin) && pinvalue.IsOn) {
						SetGPIO(pinvalue.Pin, GpioPinDriveMode.Output, GpioPinValue.High);
					}
				}
			}

			foreach (int pin in Core.Config.RelayPins) {
				Task.Delay(400).Wait();
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.Low);
			}

			await Task.Delay(500).ConfigureAwait(false);

			foreach (int pin in Core.Config.RelayPins) {
				Task.Delay(200).Wait();
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.High);
			}

			Task.Delay(800).Wait();

			foreach (int pin in Core.Config.RelayPins) {
				Task.Delay(200).Wait();
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.Low);
			}

			await Task.Delay(500).ConfigureAwait(false);

			foreach (int pin in Core.Config.RelayPins) {
				Task.Delay(400).Wait();
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.High);
			}
			return true;
		}

		public async Task<bool> RelayOneOne() {

			//make sure all relay is off
			foreach (int pin in Core.Config.RelayPins) {
				foreach (GpioPinConfig pinvalue in GpioConfigCollection) {
					if (pin.Equals(pinvalue.Pin) && pinvalue.IsOn) {
						SetGPIO(pinvalue.Pin, GpioPinDriveMode.Output, GpioPinValue.High);
					}
				}
			}

			foreach (int pin in Core.Config.RelayPins) {
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.Low);
				Task.Delay(500).Wait();
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.High);
				await Task.Delay(100).ConfigureAwait(false);
			}

			return true;
		}

		public async Task<bool> RelayOneMany() {

			//make sure all relay is off
			foreach (int pin in Core.Config.RelayPins) {
				foreach (GpioPinConfig pinvalue in GpioConfigCollection) {
					if (pin.Equals(pinvalue.Pin) && pinvalue.IsOn) {
						SetGPIO(pinvalue.Pin, GpioPinDriveMode.Output, GpioPinValue.High);
					}
				}
			}

			int counter = 0;

			foreach (int pin in Core.Config.RelayPins) {
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.Low);

				while (counter < 4) {
					Task.Delay(200).Wait();
					SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.High);
					Task.Delay(500).Wait();
					SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.Low);
					counter++;
				}

				await Task.Delay(100).ConfigureAwait(false);
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.High);
			}

			return true;
		}
	}
}
