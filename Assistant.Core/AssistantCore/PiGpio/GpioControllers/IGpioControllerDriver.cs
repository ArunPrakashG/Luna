using System;
using System.Threading.Tasks;
using static Assistant.AssistantCore.Enums;

namespace Assistant.AssistantCore.PiGpio.GpioControllers {

	/// <summary>
	/// The Gpio controller driver interface.
	/// </summary>
	internal interface IGpioControllerDriver {
		/// <summary>
		/// Indicates if the driver has been properly initialized
		/// </summary>
		bool IsDriverProperlyInitialized { get; }
		/// <summary>
		/// Get the config of the specified gpio pin. Includes pin mode and pin value.
		/// </summary>
		/// <param name="pinNumber">The pin to configure</param>
		/// <returns></returns>
		GpioPinConfig GetGpioConfig(int pinNumber);
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
		/// Sets the specified pin to specified mode and state for duration TimeSpan, after which, the pin will return to its previous state.
		/// </summary>
		/// <param name="pin">The pin to configure</param>
		/// <param name="mode">The mode to set the pin into</param>
		/// <param name="state">The state to set the pin into</param>
		/// <param name="duration">The TimeSpan duration after which the pin returns to the initial state</param>		
		/// <returns>Status of the configuration</returns>
		bool SetGpioWithTimeout(int pin, GpioPinMode mode, GpioPinState state, TimeSpan duration);
		/// <summary>
		/// Invokes shutdown on the currently loaded GpioController driver.
		/// </summary>
		void ShutdownDriver();
		/// <summary>
		/// Allows to test relay configuration.
		/// </summary>
		/// <param name="selectedCycle">The test cycle mode to run</param>
		/// <param name="singleChannelValue">Specify the pin if the test has to run on a single channel of the relay</param>
		/// <returns>Status of the test</returns>
		Task<bool> RelayTestServiceAsync(GpioCycles selectedCycle, int singleChannelValue = 0);
		/// <summary>
		/// Updates the pin configuration of the specified pin
		/// </summary>
		/// <param name="pin">The pin to update the status off.</param>
		/// <param name="mode">The new GpioPinMode value</param>
		/// <param name="value">The new GpioPinState value</param>
		/// <param name="duration">The duration to which the delayed task is set if there is any</param>
		void UpdatePinConfig(int pin, GpioPinMode mode, GpioPinState value, TimeSpan duration);
		/// <summary>
		/// Gets the physical pin number of the specified BCM pin.
		/// </summary>
		/// <param name="bcmPin">The BCM pin</param>
		/// <returns>Physical pin number</returns>
		int GpioPhysicalPinNumber(int bcmPin);
	}
}
