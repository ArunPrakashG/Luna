using Assistant.Extensions;
using Assistant.Gpio.Config;
using Assistant.Gpio.Controllers;
using Assistant.Gpio.Interfaces;
using Assistant.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Drivers {

	/// <summary>
	/// The Gpio controller driver interface.
	/// </summary>
	public interface IGpioControllerDriver {

		ILogger Logger => PinController.Logger;

		/// <summary>
		/// Indicates if the driver has been properly initialized
		/// </summary>
		bool IsDriverProperlyInitialized { get; }

		IGpioControllerDriver InitDriver(NumberingScheme scheme);

		NumberingScheme NumberingScheme { get; set; }

		EGPIO_DRIVERS DriverName { get; }

		/// <summary>
		/// The pin config.
		/// </summary>
		PinConfig PinConfig { get; }

		private bool PreExecValidation(int pin) {
			if (!IsDriverProperlyInitialized || !PiGpioController.IsAllowedToExecute) {
				return false;
			}

			if (PinConfig == null || PinConfig.PinConfigs.Count != Constants.BcmGpioPins.Length) {
				return false;
			}

			if (!PinController.IsValidPin(pin)) {
				Logger.Log("The specified pin is invalid.");
				return false;
			}

			return true;
		}

		void MapSensor(SensorMap<ISensor> _mapObj) {
			if (!PinController.IsValidPin(_mapObj.GpioPinNumber)) {
				Logger.Log("The specified pin is invalid.");
				return;
			}

			Pin? pinConfig = GetPinConfig(_mapObj.GpioPinNumber);

			if(pinConfig == null) {
				return;
			}

			if(pinConfig.SensorMap.Any(x => x.GpioPinNumber == _mapObj.GpioPinNumber && x.MapEvent == _mapObj.MapEvent)) {
				return;
			}

			pinConfig.SensorMap.Add(_mapObj);
		}

		IGpioControllerDriver CastDriver<T>(T driver) where T : IGpioControllerDriver;

		/// <summary>
		/// Get the config of the specified gpio pin. Includes pin mode and pin value.
		/// </summary>
		/// <param name="pinNumber">The pin to configure</param>
		/// <returns></returns>
		Pin? GetPinConfig(int pinNumber);

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

			Pin? pinConfig = GetPinConfig(pin);

			if (pinConfig == null) {
				return false;
			}

			if (pinConfig.Mode != GpioPinMode.Output) {
				Logger.Warning("Cannot toggle the pin as the pin mode is not set to output.");
				return false;
			}

			return SetGpioValue(pin, pinConfig.Mode, pinConfig.PinState == GpioPinState.Off ? GpioPinState.On : GpioPinState.Off);
		}

		/// <summary>
		/// Sets the specified pin to specified mode and state for duration TimeSpan, after which, the pin will return to its previous state.
		/// </summary>
		/// <param name="pin">The pin to configure</param>
		/// <param name="mode">The mode to set the pin into</param>
		/// <param name="state">The state to set the pin into</param>
		/// <param name="duration">The TimeSpan duration after which the pin returns to the initial state</param>		
		/// <returns>Status of the configuration</returns>
		bool SetGpioValue(int pin, GpioPinMode mode, GpioPinState state, TimeSpan duration) {
			if (!PreExecValidation(pin)) {
				return false;
			}

			if (SetGpioValue(pin, mode, state)) {
				UpdatePinConfig(new Pin(pin, state, mode));

				Helpers.ScheduleTask(() => {
					if (SetGpioValue(pin, mode, GpioPinState.Off)) {
						UpdatePinConfig(new Pin(pin, GpioPinState.Off, mode));
					}
				}, duration);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Invokes shutdown on the currently loaded GpioController driver.
		/// </summary>
		void ShutdownDriver() {
			if (PiGpioController.IsGracefullShutdownRequested) {
				foreach (int pin in PiGpioController.AvailablePins.OutputPins) {
					Pin? pinStatus = GetPinConfig(pin);

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

		/// <summary>
		/// Updates the pin configuration of the specified pin
		/// </summary>
		/// <param name="pin">The pin configuration object.</param>
		void UpdatePinConfig(Pin pinValue) {
			if (pinValue == null) {
				return;
			}

			if (!PreExecValidation(pinValue.PinNumber)) {
				return;
			}

			for (int i = 0; i < PinConfig.PinConfigs.Count; i++) {
				if (PinConfig.PinConfigs[i] == null || PinConfig.PinConfigs[i].PinNumber != pinValue.PinNumber) {
					continue;
				}

				PinConfig.PinConfigs[i] = pinValue;
				return;
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
			if (!PiGpioController.AvailablePins.OutputPins.Contains(pin) || !PreExecValidation(pin)) {
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

			//make sure all relay is off
			await ExecuteOnEachPin(relayPins, GpioPinMode.Output, GpioPinState.Off, 30);

			await ExecuteOnEachPin(relayPins, GpioPinMode.Output, GpioPinState.On, 400);
			await Task.Delay(500).ConfigureAwait(false);

			await ExecuteOnEachPin(relayPins, GpioPinMode.Output, GpioPinState.Off, 150);
			await Task.Delay(700).ConfigureAwait(false);

			await ExecuteOnEachPin(relayPins, GpioPinMode.Output, GpioPinState.On, 200);
			await Task.Delay(500).ConfigureAwait(false);

			return await ExecuteOnEachPin(relayPins, GpioPinMode.Output, GpioPinState.Off, 120);
		}

		async Task<bool> RelayOneOne(IEnumerable<int> relayPins) {
			if (relayPins.Count() <= 0) {
				Logger.Warning("No pins specified.");
				return false;
			}

			//make sure all relay is off
			await ExecuteOnEachPin(relayPins, GpioPinMode.Output, GpioPinState.Off, 50);

			foreach (int pin in relayPins) {
				SetGpioValue(pin, GpioPinMode.Output, GpioPinState.On);
				await Task.Delay(500).ConfigureAwait(false);
				SetGpioValue(pin, GpioPinMode.Output, GpioPinState.Off);
				await Task.Delay(100).ConfigureAwait(false);
			}

			return true;
		}

		async Task<bool> RelayOneMany(IEnumerable<int> relayPins) {
			if (relayPins.Count() <= 0) {
				Logger.Warning("No pins specified.");
				return false;
			}

			//make sure all relay is off
			await ExecuteOnEachPin(relayPins, GpioPinMode.Output, GpioPinState.Off, 50);

			foreach (int pin in relayPins) {
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

		async Task<bool> ExecuteOnEachPin(IEnumerable<int> pins, GpioPinMode setMode, GpioPinState setPinState, int delayInMs = 100) {
			if (pins == null || pins.Count() <= 0) {
				return false;
			}

			foreach (int pin in pins) {
				if (!PiGpioController.AvailablePins.OutputPins.Contains(pin) || !PreExecValidation(pin)) {
					continue;
				}

				Pin? pinConfig = GetPinConfig(pin);

				if (pinConfig == null) {
					return false;
				}

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
