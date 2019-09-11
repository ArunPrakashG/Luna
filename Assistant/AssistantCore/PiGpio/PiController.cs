
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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.WiringPi;

namespace Assistant.AssistantCore.PiGpio {
	public class GpioPinConfig {
		[JsonProperty]
		public int Pin { get; set; } = 0;

		[JsonProperty]
		public GpioPinValue PinValue { get; set; }

		[JsonProperty]
		public GpioPinDriveMode Mode { get; set; }

		[JsonProperty]
		public bool IsPinOn => PinValue == GpioPinValue.High ? false : true;
	}

	public class PiController {
		private readonly Logger Logger = new Logger("PI-CONTROLLER");

		public GpioEventManager GpioPollingManager { get; private set; }

		public GpioMorseTranslator MorseTranslator { get; private set; }

		public BluetoothController PiBluetooth { get; private set; }

		public SoundController PiSound { get; private set; }

		public static bool IsProperlyInitialized { get; private set; }

		public PiController(bool withPolling) {
			if (Core.DisablePiMethods || Core.RunningPlatform != OSPlatform.Linux) {
				Logger.Log("Failed to start PiController.", Enums.LogLevels.Warn);
				return;
			}

			Pi.Init<BootstrapWiringPi>();
			GpioPollingManager = new GpioEventManager(this);
			MorseTranslator = new GpioMorseTranslator(this);
			PiBluetooth = new BluetoothController();
			PiSound = new SoundController();
			ControllerHelpers.DisplayPiInfo();

			if (withPolling) {
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

					foreach (GpioEventGenerator gen in GpioPollingManager.GpioPinEventGenerators) {
						gen.GPIOPinValueChanged += OnGpioPinValueChanged;
					}
				}, TimeSpan.FromSeconds(5));
			}

			IsProperlyInitialized = true;
			Logger.Log("Initiated GPIO Controller class!", Enums.LogLevels.Trace);
		}

		private void OnGpioPinValueChanged(object sender, GpioPinValueChangedEventArgs e) {
			switch (e.PinState) {
				case Enums.GpioPinEventStates.OFF when e.PinPreviousState == Enums.GpioPinEventStates.OFF:
					Logger.Log($"{e.PinNumber} gpio pin set to OFF state. (OFF)", Enums.LogLevels.Info);
					break;

				case Enums.GpioPinEventStates.ON when e.PinPreviousState == Enums.GpioPinEventStates.ON:
					Logger.Log($"{e.PinNumber} gpio pin set to ON state. (ON)", Enums.LogLevels.Info);
					break;

				case Enums.GpioPinEventStates.ON when e.PinPreviousState == Enums.GpioPinEventStates.OFF:
					Logger.Log($"{e.PinNumber} gpio pin set to ON state. (OFF)", Enums.LogLevels.Info);
					break;

				case Enums.GpioPinEventStates.OFF when e.PinPreviousState == Enums.GpioPinEventStates.ON:
					Logger.Log($"{e.PinNumber} gpio pin set to OFF state. (ON)", Enums.LogLevels.Info);
					break;

				default:
					Logger.Log($"Value for {e.PinNumber} pin changed to {e.PinCurrentDigitalValue} from {e.PinPreviousDigitalValue.ToString()}");
					break;
			}
		}

		public GpioPinConfig GetPinConfig(int pinToCheck) {
			(int pin, GpioPinDriveMode driveMode, GpioPinValue pinValue) = GetGpio(pinToCheck);
			GpioPinConfig result = new GpioPinConfig() {
				PinValue = pinValue,
				Mode = driveMode,
				Pin = pin
			};

			return result;
		}

		public (int pin, GpioPinDriveMode driveMode, GpioPinValue pinValue) GetGpio(int pinNumber) =>
			(pinNumber, Pi.Gpio[pinNumber].PinMode, Pi.Gpio[pinNumber].Read() ? GpioPinValue.High : GpioPinValue.Low);

		public async Task<bool> SetGpioWithTimeout(int pin, GpioPinDriveMode mode, GpioPinValue state, int timeoutDuration) {
			if (pin <= 0 || timeoutDuration <= 0) {
				return false;
			}

			if (SetGpioValue(pin, mode, state)) {
				await Task.Delay(timeoutDuration).ConfigureAwait(false);
				return SetGpioValue(pin, mode, GpioPinValue.High);
			}
			return false;
		}

		public bool GpioDigitalRead(int pin, bool onlyInputPins = false) {
			IGpioPin gpioPin = Pi.Gpio[pin];

			if (onlyInputPins && gpioPin.PinMode != GpioPinDriveMode.Input) {
				Logger.Log("The specified gpio pin mode isn't set to Input.", Enums.LogLevels.Error);
				return false;
			}

			return gpioPin.Read();
		}

		public bool SetGpioValue(int pin, GpioPinDriveMode mode, GpioPinValue state) {
			if (!Core.Config.EnableGpioControl) {
				return false;
			}

			IGpioPin GpioPin = Pi.Gpio[pin];
			GpioPin.PinMode = mode;

			if(mode == GpioPinDriveMode.Output) {
				GpioPin.Write(state);
				Logger.Log($"Configured ({pin}) gpio pin to ({state.ToString()}) state with ({mode.ToString()}) mode.", Enums.LogLevels.Trace);
				return true;
			}

			Logger.Log($"Configured ({pin}) gpio pin with ({mode.ToString()}) mode.", Enums.LogLevels.Trace);
			return true;
		}

		public void InitGpioShutdownTasks() {
			if (GpioPollingManager != null) {
				foreach (GpioEventGenerator c in GpioPollingManager.GpioPinEventGenerators) {
					c.GPIOPinValueChanged -= OnGpioPinValueChanged;
				}
			}

			GpioPollingManager.ExitEventGenerator();

			if (Core.Config.CloseRelayOnShutdown) {
				foreach (int pin in Core.Config.RelayPins) {
					GpioPinConfig pinStatus = GetPinConfig(pin);
					if (pinStatus.PinValue == GpioPinValue.Low) {
						SetGpioValue(pin, GpioPinDriveMode.Output, GpioPinValue.High);
					}
				}
			}
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
			SetGpioValue(pin, GpioPinDriveMode.Output, GpioPinValue.Low);
			Logger.Log($"Waiting for {delayInMs} ms to close the relay...");
			await Task.Delay(delayInMs).ConfigureAwait(false);
			SetGpioValue(pin, GpioPinDriveMode.Output, GpioPinValue.High);
			Logger.Log("Relay closed!");
			return true;
		}

		public async Task<bool> RelayOneTwo() {
			//make sure all relay is off
			foreach (int pin in Core.Config.RelayPins) {
				SetGpioValue(pin, GpioPinDriveMode.Output, GpioPinValue.High);
			}

			foreach (int pin in Core.Config.RelayPins) {
				Task.Delay(400).Wait();
				SetGpioValue(pin, GpioPinDriveMode.Output, GpioPinValue.Low);
			}

			await Task.Delay(500).ConfigureAwait(false);

			foreach (int pin in Core.Config.RelayPins) {
				Task.Delay(200).Wait();
				SetGpioValue(pin, GpioPinDriveMode.Output, GpioPinValue.High);
			}

			Task.Delay(800).Wait();

			foreach (int pin in Core.Config.RelayPins) {
				Task.Delay(200).Wait();
				SetGpioValue(pin, GpioPinDriveMode.Output, GpioPinValue.Low);
			}

			await Task.Delay(500).ConfigureAwait(false);

			foreach (int pin in Core.Config.RelayPins) {
				Task.Delay(400).Wait();
				SetGpioValue(pin, GpioPinDriveMode.Output, GpioPinValue.High);
			}
			return true;
		}

		public async Task<bool> RelayOneOne() {
			//make sure all relay is off
			foreach (int pin in Core.Config.RelayPins) {
				SetGpioValue(pin, GpioPinDriveMode.Output, GpioPinValue.High);
			}

			foreach (int pin in Core.Config.RelayPins) {
				SetGpioValue(pin, GpioPinDriveMode.Output, GpioPinValue.Low);
				Task.Delay(500).Wait();
				SetGpioValue(pin, GpioPinDriveMode.Output, GpioPinValue.High);
				await Task.Delay(100).ConfigureAwait(false);
			}

			return true;
		}

		public async Task<bool> RelayOneMany() {
			//make sure all relay is off
			foreach (int pin in Core.Config.RelayPins) {
				SetGpioValue(pin, GpioPinDriveMode.Output, GpioPinValue.High);
			}

			int counter = 0;

			foreach (int pin in Core.Config.RelayPins) {
				SetGpioValue(pin, GpioPinDriveMode.Output, GpioPinValue.Low);

				while (counter < 4) {
					Task.Delay(200).Wait();
					SetGpioValue(pin, GpioPinDriveMode.Output, GpioPinValue.High);
					Task.Delay(500).Wait();
					SetGpioValue(pin, GpioPinDriveMode.Output, GpioPinValue.Low);
					counter++;
				}

				await Task.Delay(100).ConfigureAwait(false);
				SetGpioValue(pin, GpioPinDriveMode.Output, GpioPinValue.High);
			}

			return true;
		}
	}
}