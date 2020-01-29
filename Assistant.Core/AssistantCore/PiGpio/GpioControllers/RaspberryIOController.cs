using Assistant.Extensions;
using Assistant.Log;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.WiringPi;
using static Assistant.AssistantCore.Enums;

namespace Assistant.AssistantCore.PiGpio.GpioControllers {
	internal class RaspberryIOController : IGpioControllerDriver {
		private readonly GpioPinController GpioController;
		private readonly Logger Logger;
		public bool IsDriverProperlyInitialized { get; private set; }

		internal RaspberryIOController(GpioPinController gpioController) {
			GpioController = gpioController ?? throw new ArgumentNullException(nameof(gpioController), "The Gpio Controller cannot be null!");
			Logger = GpioController.Logger;
		}

		[CanBeNull]
		internal RaspberryIOController InitDriver() {
			if (Core.DisablePiMethods || Core.RunningPlatform != OSPlatform.Linux || !Core.Config.EnableGpioControl) {
				Logger.Log("Failed to initialize Gpio Controller Driver.", LogLevels.Warn);
				IsDriverProperlyInitialized = false;
				return this;
			}

			Pi.Init<BootstrapWiringPi>();
			IsDriverProperlyInitialized = true;
			return this;
		}

		[CanBeNull]
		public GpioPinConfig GetGpioConfig(int pinNumber) {
			if (Core.DisablePiMethods || !IsDriverProperlyInitialized) {
				return new GpioPinConfig();
			}

			if (!PiController.IsValidPin(pinNumber)) {
				Logger.Log("The specified pin is invalid.");
				return new GpioPinConfig();
			}

			GpioPin pin = (GpioPin) Pi.Gpio[pinNumber];
			return new GpioPinConfig(pinNumber, (GpioPinState) pin.ReadValue(), (GpioPinMode) pin.PinMode, false, 0);
		}

		public bool SetGpioWithTimeout(int pin, GpioPinMode mode, GpioPinState state, TimeSpan duration) {
			if (!PiController.IsValidPin(pin)) {
				Logger.Log("The specified pin is invalid.");
				return false;
			}

			if (PiController.PinConfigCollection.Count > 0 && PiController.PinConfigCollection.Where(x => x.Pin == pin && x.IsDelayedTaskSet).Any()) {
				return false;
			}

			if (SetGpioValue(pin, mode, state)) {
				UpdatePinConfig(pin, mode, state, duration);

				Helpers.ScheduleTask(() => {
					if (SetGpioValue(pin, mode, GpioPinState.Off)) {
						UpdatePinConfig(pin, mode, GpioPinState.Off, TimeSpan.Zero);
					}
				}, duration);

				return true;
			}

			return false;
		}

		public void UpdatePinConfig(int pin, GpioPinMode mode, GpioPinState value, TimeSpan duration) {
			if (!PiController.IsValidPin(pin)) {
				Logger.Log("The specified pin is invalid.");
				return;
			}

			if (PiController.PinConfigCollection.Count <= 0) {
				return;
			}

			foreach (GpioPinConfig config in PiController.PinConfigCollection) {
				if (config.Pin == pin) {
					config.IsDelayedTaskSet = duration != TimeSpan.Zero;
					config.TaskSetAfterMinutes = duration != TimeSpan.Zero ? duration.Minutes : 0;
					config.Mode = mode;
					config.PinValue = value;
					break;
				}
			}
		}

		public bool SetGpioValue(int pin, GpioPinMode mode) {
			if (!PiController.IsValidPin(pin)) {
				return false;
			}

			GpioPin GpioPin = (GpioPin) Pi.Gpio[pin];
			GpioPin.PinMode = (GpioPinDriveMode) mode;
			Logger.Log($"Configured ({pin}) gpio pin with ({mode.ToString()}) mode.", LogLevels.Trace);
			return true;
		}

		public bool SetGpioValue(int pin, GpioPinState state) {
			if (!PiController.IsValidPin(pin)) {
				return false;
			}

			GpioPin GpioPin = (GpioPin) Pi.Gpio[pin];

			if (GpioPin.PinMode == GpioPinDriveMode.Output) {
				GpioPin.Write((GpioPinValue) state);
				Logger.Log($"Configured ({pin}) gpio pin to ({state.ToString()}) state.", LogLevels.Trace);
				return true;
			}

			return false;
		}

		public bool SetGpioValue(int pin, GpioPinMode mode, GpioPinState state) {
			if (!PiController.IsValidPin(pin)) {
				return false;
			}

			GpioPin GpioPin = (GpioPin) Pi.Gpio[pin];
			GpioPin.PinMode = (GpioPinDriveMode) mode;

			if (mode == GpioPinMode.Output) {
				GpioPin.Write((GpioPinValue) state);
				Logger.Log($"Configured ({pin}) gpio pin to ({state.ToString()}) state with ({mode.ToString()}) mode.", LogLevels.Trace);
				return true;
			}

			Logger.Log($"Configured ({pin}) gpio pin with ({mode.ToString()}) mode.", LogLevels.Trace);
			return true;
		}

		public void ShutdownDriver() {
			if (Core.Config.CloseRelayOnShutdown) {
				foreach (int pin in Core.Config.OutputModePins) {
					GpioPinConfig? pinStatus = GetGpioConfig(pin);

					if (pinStatus == null) {
						continue;
					}

					if (pinStatus.IsPinOn) {
						SetGpioValue(pin, GpioPinMode.Output, GpioPinState.Off);
						Logger.Log($"Closed pin {pin} as part of shutdown process.");
					}
				}
			}
		}

		private async Task<bool> ExecuteOnEachPin(IEnumerable<int> pins, GpioPinMode setMode, GpioPinState setPinState, int delayInMs = 100) {
			if (pins == null || pins.Count() <= 0) {
				return false;
			}

			foreach (int pin in pins) {
				GpioPinConfig? pinConfig = GetGpioConfig(pin);

				if (pinConfig == null) {
					continue;
				}

				if (pinConfig.Mode != setMode) {
					SetGpioValue(pin, setMode);
				}

				if (pinConfig.PinValue != setPinState) {
					SetGpioValue(pin, setPinState);
				}

				await Task.Delay(delayInMs).ConfigureAwait(false);
			}

			return true;
		}

		public async Task<bool> RelayTestServiceAsync(GpioCycles selectedCycle, int singleChannelValue = 0) {
			Logger.Log("Relay test service started!");

			switch (selectedCycle) {
				case GpioCycles.OneTwo:
					return await RelayOneTwo().ConfigureAwait(false);

				case GpioCycles.OneOne:
					return await RelayOneOne().ConfigureAwait(false);

				case GpioCycles.OneMany:
					return await RelayOneMany().ConfigureAwait(false);

				case GpioCycles.Cycle:
					return await RelayOneTwo().ConfigureAwait(false) && await RelayOneOne().ConfigureAwait(false) && await RelayOneMany().ConfigureAwait(false);

				case GpioCycles.Single:
					return await RelaySingle(singleChannelValue, 8000).ConfigureAwait(false);

				case GpioCycles.Base:
					Logger.Log("Base argument specified, running default cycle test!");
					goto case GpioCycles.Cycle;
				case GpioCycles.Default:
					Logger.Log("Unknown value, Aborting...");
					break;
			}

			Logger.Log("One or more tests failed.", LogLevels.Warn);
			return false;
		}

		protected async Task<bool> RelaySingle(int pin = 0, int delayInMs = 8000) {
			if (!Core.Config.RelayPins.Contains(pin)) {
				return false;
			}

			SetGpioValue(pin, GpioPinMode.Output, GpioPinState.On);
			Logger.Log($"Waiting for {delayInMs} ms to close the relay...");
			await Task.Delay(delayInMs).ConfigureAwait(false);
			SetGpioValue(pin, GpioPinMode.Output, GpioPinState.Off);
			Logger.Log("Relay closed!");
			"Relay single test passed!".LogInfo(Logger);
			return true;
		}

		protected async Task<bool> RelayOneTwo() {
			//make sure all relay is off
			await ExecuteOnEachPin(Core.Config.RelayPins, GpioPinMode.Output, GpioPinState.Off, 30);

			await ExecuteOnEachPin(Core.Config.RelayPins, GpioPinMode.Output, GpioPinState.On, 400);
			await Task.Delay(500).ConfigureAwait(false);

			await ExecuteOnEachPin(Core.Config.RelayPins, GpioPinMode.Output, GpioPinState.Off, 150);
			await Task.Delay(700).ConfigureAwait(false);

			await ExecuteOnEachPin(Core.Config.RelayPins, GpioPinMode.Output, GpioPinState.On, 200);
			await Task.Delay(500).ConfigureAwait(false);

			return await ExecuteOnEachPin(Core.Config.RelayPins, GpioPinMode.Output, GpioPinState.Off, 120);
		}

		protected async Task<bool> RelayOneOne() {
			//make sure all relay is off
			await ExecuteOnEachPin(Core.Config.RelayPins, GpioPinMode.Output, GpioPinState.Off, 50);

			foreach (int pin in Core.Config.RelayPins) {
				SetGpioValue(pin, GpioPinMode.Output, GpioPinState.On);
				await Task.Delay(500).ConfigureAwait(false);
				SetGpioValue(pin, GpioPinMode.Output, GpioPinState.Off);
				await Task.Delay(100).ConfigureAwait(false);
			}

			return true;
		}

		protected async Task<bool> RelayOneMany() {
			//make sure all relay is off
			await ExecuteOnEachPin(Core.Config.RelayPins, GpioPinMode.Output, GpioPinState.Off, 50);

			foreach (int pin in Core.Config.RelayPins) {
				SetGpioValue(pin, GpioPinMode.Output, GpioPinState.On);

				for (int i = 0; i <= 5; i++) {
					await Task.Delay(200).ConfigureAwait(false);
					SetGpioValue(pin, GpioPinMode.Output, GpioPinState.Off);
					await Task.Delay(500).ConfigureAwait(false);
					SetGpioValue(pin, GpioPinMode.Output, GpioPinState.On);
				}

				SetGpioValue(pin, GpioPinMode.Output, GpioPinState.Off);
			}

			return true;
		}

		public GpioPinState GpioPinStateRead(int pin) {
			if (!PiController.IsValidPin(pin)) {
				Logger.Log("The specified pin is invalid.");
				return GpioPinState.Off;
			}

			GpioPin gpioPin = (GpioPin) Pi.Gpio[pin];
			return (GpioPinState) gpioPin.ReadValue();
		}

		public bool GpioDigitalRead(int pin) {
			if (!PiController.IsValidPin(pin)) {
				Logger.Log("The specified pin is invalid.");
				return false;
			}

			GpioPin gpioPin = (GpioPin) Pi.Gpio[pin];
			return gpioPin.Read();
		}

		public int GpioPhysicalPinNumber(int bcmPin) {
			if (!PiController.IsValidPin(bcmPin)) {
				Logger.Log("The specified pin is invalid.");
				return 0;
			}

			GpioPin GpioPin = (GpioPin) Pi.Gpio[bcmPin];
			return GpioPin.PhysicalPinNumber;
		}
	}
}
