namespace Luna.Gpio.Config {
	using System;
	using System.Collections.Generic;
	using System.Text;
	using static Luna.Gpio.Enums;

	/// <summary>
	/// Defines pin configuration collection.
	/// </summary>
	public class PinConfig {

		public bool SafeMode;

		/// <summary>
		/// Defines the PinConfigs
		/// </summary>		
		public readonly List<Pin> PinConfigs;

		/// <summary>
		/// Initializes a new instance of the <see cref="PinConfig"/> class.
		/// </summary>
		/// <param name="configs">The configs<see cref="List{Pin}"/></param>
		public PinConfig(List<Pin> configs, bool safeMode) {
			PinConfigs = configs;
			SafeMode = safeMode;
		}

		public Pin this[int index] => PinConfigs[index];
	}

	/// <summary>
	/// Defines the pin configuration of the pin it holds.
	/// </summary>	
	public struct Pin {
		/// <summary>
		/// The pin.
		/// </summary>		
		public readonly int PinNumber;

		/// <summary>
		/// Gets or sets the Pin state. (On/Off)
		/// </summary>		
		public GpioPinState PinState;

		/// <summary>
		/// Gets or sets the Pin mode. (Output/Input)
		/// </summary>		
		public GpioPinMode Mode;

		/// <summary>
		/// Gets or sets a value indicating whether the pin is available.
		/// </summary>		
		public bool IsAvailable;

		/// <summary>
		/// Gets or sets the Scheduler job name if the pin isn't available.
		/// </summary>		
		public string? JobName { get; set; }

		/// <summary>
		/// Gets a value indicating whether IsPinOn
		/// Gets a value indicating the pin current state. <see cref="PinState"/>
		/// </summary>		
		public bool IsPinOn => PinState == GpioPinState.On;

		/// <summary>
		/// Initializes a new instance of the <see cref="Pin"/> class.
		/// </summary>
		/// <param name="pin">The pin <see cref="int"/></param>
		/// <param name="state">The state <see cref="GpioPinState"/></param>
		/// <param name="mode">The mode <see cref="GpioPinMode"/></param>
		/// <param name="available">The status if the pin is currently available <see cref="bool"/></param>
		/// <param name="jobName">The jobName of the scheduler if the pin isn't available at the moment.<see cref="string?"/></param>
		public Pin(int pin, GpioPinState state, GpioPinMode mode, bool available = true, string? jobName = null) {
			PinNumber = pin;
			PinState = state;
			Mode = mode;
			IsAvailable = available;
			JobName = jobName;
		}

		public Pin(int pin, GpioPinMode mode, bool available = true, string? jobName = null) {
			PinNumber = pin;
			PinState = GpioPinState.Off;
			Mode = mode;
			IsAvailable = available;
			JobName = jobName;
		}

		public Pin(int pin, GpioPinState state, bool available = true, string? jobName = null) {
			PinNumber = pin;
			PinState = state;
			Mode = GpioPinMode.Input;
			IsAvailable = available;
			JobName = jobName;
		}

		/// <summary>
		/// Gets a summary of the pin configuration this object holds.
		/// </summary>
		/// <returns>The <see cref="string"/></returns>
		public override string ToString() {
			StringBuilder s = new StringBuilder();
			s.AppendLine("---------------------------");
			s.AppendLine($"Pin -> {PinNumber}");
			s.AppendLine($"Pin Value -> {PinState}");
			s.AppendLine($"Pin Mode -> {Mode}");
			s.AppendLine($"Is Tasked -> {!IsAvailable}");
			s.AppendLine($"Task Name -> {JobName}");
			s.AppendLine($"Is pin on -> {IsPinOn}");
			s.AppendLine("---------------------------");
			return s.ToString();
		}

		/// <summary>
		/// Compares both objects.
		/// </summary>
		/// <param name="obj">The obj<see cref="object?"/></param>
		/// <returns>The <see cref="bool"/></returns>
		public override bool Equals(object? obj) {
			if (obj == null) {
				return false;
			}

			Pin config = (Pin) obj;
			return config.PinNumber == PinNumber;
		}

		/// <summary>
		/// Gets the hash code of the object.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => base.GetHashCode();
	}
}
