using Luna.Extensions;
using Luna.Gpio.Config;
using Luna.Gpio.Controllers;
using Luna.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Luna.Gpio.Enums;

namespace Luna.Gpio.Drivers {
	/// <summary>
	/// The Gpio controller driver interface.
	/// </summary>
	public interface IGpioControllerDriver {
		/// <summary>
		/// Internal Logger instance.
		/// </summary>
		ILogger Logger { get; }

		/// <summary>
		/// Indicates if the driver has been initialized
		/// </summary>
		bool IsDriverInitialized { get; }

		/// <summary>
		/// All the available pins on the device.
		/// </summary>
		AvailablePins AvailablePins { get; }

		/// <summary>
		/// The numbering scheme to use for the supported drivers.
		/// </summary>
		NumberingScheme NumberingScheme { get; }

		/// <summary>
		/// The driver name.
		/// </summary>
		GpioDriver DriverName { get; }

		/// <summary>
		/// The pin config.
		/// </summary>
		PinConfig PinConfig { get; }

		/// <summary>
		/// Initializes the gpio driver.
		/// </summary>
		/// /// <param name="_availablePins">The available pins on the device.</param>
		/// <param name="_scheme">The numbering scheme to use.</param>
		/// <returns>The driver.</returns>
		public IGpioControllerDriver InitDriver(ILogger _logger, AvailablePins _availablePins, NumberingScheme _scheme);

		/// <summary>
		/// Some validation which have to ran before any of the pin related functions execute in the driver.
		/// </summary>
		/// <param name="pin"></param>
		/// <returns></returns>
		private bool PreExecValidation(int pin) {
			if (!IsDriverInitialized || !GpioCore.IsAllowedToExecute) {
				return false;
			}

			if (PinConfig.PinConfigs.Count != Constants.BcmGpioPins.Length) {
				return false;
			}

			if (!PinController.IsValidPin(pin)) {
				return false;
			}

			return true;
		}

		/// <summary>
		/// Recasts the driver.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="driver"></param>
		/// <returns></returns>
		IGpioControllerDriver Cast<T>(T driver) where T : IGpioControllerDriver;

		/// <summary>
		/// Get the config of the specified gpio pin. Includes pin mode and pin value.
		/// </summary>
		/// <param name="pinNumber">The pin to configure</param>
		/// <returns></returns>
		Pin GetPinConfig(int pinNumber);

		/// <summary>
		/// Sets the GpioPinMode of the specified pin.
		/// </summary>
		/// <param name="pin">The pin to configure</param>
		/// <param name="mode">The mode to set the pin into</param>
		/// <returns>Status of the configuration</returns>
		bool SetGpioValue(int pin, GpioPinMode mode);

		/// <summary>
		/// Sets the GpioPinMode and GpioPinState of the specified pin.
		/// </summary>
		/// <param name="pin">The pin to configure</param>
		/// <param name="mode">The mode to set the pin into</param>
		/// <param name="state">The state to set the pin into</param>
		/// <returns>Status of the configuration</returns>
		bool SetGpioValue(int pin, GpioPinMode mode, GpioPinState state);

		/// <summary>
		/// Reads the GpioPinState value of the specified pin.
		/// </summary>
		/// <param name="pin">The pin to read the value from</param>
		/// <returns>The GpioPinState value</returns>
		GpioPinState GpioPinStateRead(int pin);

		/// <summary>
		/// Reads the digital value of the specified pin.
		/// </summary>
		/// <param name="pin">The pin to read the value from</param>
		/// <returns>The digital boolean value</returns>
		bool GpioDigitalRead(int pin);

		/// <summary>
		/// Sets the GpioPinState of the specified pin.
		/// </summary>
		/// <param name="pin">The pin to configure</param>
		/// <param name="state">The state to set the pin into</param>
		/// <returns>Status of the configuration</returns>
		bool SetGpioValue(int pin, GpioPinState state);

		/// <summary>
		/// Toggles the pin into the opposite state of what it is at the present.
		/// if its in On state, it will be changed to Off state and vice versa.
		/// </summary>
		/// <param name="pin">The pin to toggle.</param>
		/// <returns>Success or failure boolean value.</returns>
		bool TogglePinState(int pin) {
			if (!PreExecValidation(pin)) {
				return false;
			}

			Pin pinConfig = GetPinConfig(pin);

			if (pinConfig.Mode != GpioPinMode.Output) {
				return false;
			}

			return SetGpioValue(pin, pinConfig.Mode, pinConfig.PinState == GpioPinState.Off ? GpioPinState.On : GpioPinState.Off);
		}

		/// <summary>
		/// Sets the specified pin to specified mode and state for duration TimeSpan, after which, the pin will return to its previous state.
		/// <br><b>NOTE: This will block the calling thread until the timespan expires.</b></br>
		/// </summary>
		/// <param name="pin">The pin to configure</param>
		/// <param name="mode">The mode to set the pin into</param>
		/// <param name="state">The state to set the pin into</param>
		/// <param name="duration">The TimeSpan duration after which the pin returns to the initial state</param>
		/// <param name="shouldBlockThread">Specifies if the method should wait until the duration expires.</param>
		/// <returns>Status of the configuration</returns>
		bool SetGpioValue(int pin, GpioPinMode mode, GpioPinState state, TimeSpan duration, bool shouldBlockThread = false) {
			if (!PreExecValidation(pin)) {
				return false;
			}

			if (SetGpioValue(pin, mode, state)) {
				bool set = false;

				Helpers.ScheduleTask(() => {
					SetGpioValue(pin, mode, GpioPinState.Off);
					set = true;
				}, duration);

				Helpers.WaitWhile(() => shouldBlockThread && !set);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Invokes shutdown on the currently loaded GpioController driver.
		/// </summary>
		void ShutdownDriver(bool _gracefullShutdownRequested = true) {
			if (!_gracefullShutdownRequested) {
				return;
			}

			for (int i = 0; i < AvailablePins.GpioPins.Length; i++) {
				Pin pinStatus = GetPinConfig(AvailablePins.GpioPins[i]);

				if (pinStatus.IsPinOn) {
					SetGpioValue(pinStatus.PinNumber, GpioPinMode.Output, GpioPinState.Off);
				}
			}
		}

		/// <summary>
		/// Gets the physical pin number of the specified BCM pin.
		/// </summary>
		/// <param name="bcmPin">The BCM pin</param>
		/// <returns>Physical pin number</returns>
		int GpioPhysicalPinNumber(int bcmPin);

		/// <summary>
		/// Allows to test relay configuration.
		/// </summary>
		/// <param name="relayPins">The pins to run the test on.</param>
		/// <param name="selectedCycle">The test cycle mode to run</param>
		/// <param name="singleChannelValue">Specify the pin if the test has to run on a single channel of the relay</param>
		/// <returns>Status of the test</returns>
		async Task<bool> RelayTestAsync(IEnumerable<int> relayPins, GpioCycles selectedCycle, int singleChannelValue = 0) {
			if (relayPins.Count() <= 0) {
				Logger.Warning("No pins specified.");
				return false;
			}

			Logger.Log("Relay test service started!");

			switch (selectedCycle) {
				case GpioCycles.OneTwo:
					return await RelayOneTwo(relayPins).ConfigureAwait(false);

				case GpioCycles.OneOne:
					return await RelayOneOne(relayPins).ConfigureAwait(false);

				case GpioCycles.OneMany:
					return await RelayOneMany(relayPins).ConfigureAwait(false);

				case GpioCycles.Cycle:
					return await RelayOneTwo(relayPins).ConfigureAwait(false) &&
						await RelayOneOne(relayPins).ConfigureAwait(false) &&
						await RelayOneMany(relayPins).ConfigureAwait(false);

				case GpioCycles.Single:
					return await RelaySingle(singleChannelValue, 8000).ConfigureAwait(false);

				case GpioCycles.Base:
					goto case GpioCycles.Cycle;
				case GpioCycles.Default:
					Logger.Log("Unknown value, Aborting...");
					break;
			}

			Logger.Warning("One or more tests failed.");
			return false;
		}

		async Task<bool> RelaySingle(int pin = 0, int delayInMs = 8000) {
			if (!AvailablePins.OutputPins.Contains(pin) || !PreExecValidation(pin)) {
				return false;
			}

			SetGpioValue(pin, GpioPinMode.Output, GpioPinState.On);
			Logger.Log($"Waiting for {delayInMs} ms to close the relay...");
			await Task.Delay(delayInMs).ConfigureAwait(false);
			SetGpioValue(pin, GpioPinMode.Output, GpioPinState.Off);
			Logger.Log("Relay closed!");
			Logger.Info("Relay single test passed!");
			return true;
		}

		async Task<bool> RelayOneTwo(IEnumerable<int> relayPins) {
			if (relayPins.Count() <= 0) {
				Logger.Warning("No pins specified.");
				return false;
			}

			// make sure all relay is off
			await ExecuteOnEachPin(relayPins, GpioPinMode.Output, GpioPinState.Off, 100);

			await ExecuteOnEachPin(relayPins, GpioPinMode.Output, GpioPinState.On, 400);
			await Task.Delay(1000).ConfigureAwait(false);
			await ExecuteOnEachPin(relayPins, GpioPinMode.Output, GpioPinState.Off, 200);
			await Task.Delay(1000).ConfigureAwait(false);
			await ExecuteOnEachPin(relayPins, GpioPinMode.Output, GpioPinState.On, 800);
			await Task.Delay(1000).ConfigureAwait(false);
			return await ExecuteOnEachPin(relayPins, GpioPinMode.Output, GpioPinState.Off, 200);
		}

		async Task<bool> RelayOneOne(IEnumerable<int> relayPins) {
			if (relayPins.Count() <= 0) {
				Logger.Warning("No pins specified.");
				return false;
			}

			// make sure all relay is off
			await ExecuteOnEachPin(relayPins, GpioPinMode.Output, GpioPinState.Off, 100);

			for (int i = 0; i < relayPins.Count(); i++) {
				SetGpioValue(relayPins.ElementAt(i), GpioPinMode.Output, GpioPinState.On);
				await Task.Delay(1000).ConfigureAwait(false);
				SetGpioValue(relayPins.ElementAt(i), GpioPinMode.Output, GpioPinState.Off);
				await Task.Delay(500).ConfigureAwait(false);
			}

			return true;
		}

		async Task<bool> RelayOneMany(IEnumerable<int> relayPins) {
			if (relayPins.Count() <= 0) {
				Logger.Warning("No pins specified.");
				return false;
			}

			// make sure all relay is off
			await ExecuteOnEachPin(relayPins, GpioPinMode.Output, GpioPinState.Off, 100);

			for (int i = 0; i < relayPins.Count(); i++) {
				int pin = relayPins.ElementAt(i);

				SetGpioValue(pin, GpioPinMode.Output, GpioPinState.On);

				for (int j = 0; j < 5; j++) {
					await Task.Delay(500).ConfigureAwait(false);
					SetGpioValue(pin, GpioPinMode.Output, GpioPinState.Off);
					await Task.Delay(500).ConfigureAwait(false);
					SetGpioValue(pin, GpioPinMode.Output, GpioPinState.On);
					await Task.Delay(500).ConfigureAwait(false);
				}

				SetGpioValue(pin, GpioPinMode.Output, GpioPinState.Off);
			}

			return true;
		}

		async Task<bool> ExecuteOnEachPin(IEnumerable<int> pins, GpioPinMode setMode, GpioPinState setPinState, int delayInMs = 100) {
			if (pins == null || pins.Count() <= 0) {
				return false;
			}

			foreach (int pin in pins) {
				if (!AvailablePins.OutputPins.Contains(pin) || !PreExecValidation(pin)) {
					continue;
				}

				Pin pinConfig = GetPinConfig(pin);

				if (pinConfig.Mode != setMode) {
					SetGpioValue(pin, setMode);
				}

				if (pinConfig.PinState != setPinState) {
					SetGpioValue(pin, setPinState);
				}

				await Task.Delay(delayInMs).ConfigureAwait(false);
			}

			return true;
		}
	}
}
