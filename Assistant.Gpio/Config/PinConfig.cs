namespace Assistant.Gpio.Config {
	using Newtonsoft.Json;
	using System;
	using System.Collections.Generic;
	using System.Text;
	using static Assistant.Gpio.Controllers.PiController;

	/// <summary>
	/// Defines pin configuration collection.
	/// </summary>
	[Serializable]
	public class PinConfig {
		/// <summary>
		/// Defines the pin configuration of the pin it holds.
		/// </summary>
		public class Pin {
			/// <summary>
			/// The pin.
			/// </summary>
			[JsonProperty]
			public readonly int PinNumber;

			/// <summary>
			/// Gets or sets the Pin state. (On/Off)
			/// </summary>
			[JsonProperty]
			public GpioPinState PinState { get; set; } = GpioPinState.Off;

			/// <summary>
			/// Gets or sets the Pin mode. (Output/Input)
			/// </summary>
			[JsonProperty]
			public GpioPinMode Mode { get; set; } = GpioPinMode.Input;

			/// <summary>
			/// Gets or sets a value indicating whether the pin is available.
			/// </summary>
			[JsonProperty]
			public bool IsAvailable { get; set; } = true;

			/// <summary>
			/// Gets or sets the Scheduler job name if the pin isn't available.
			/// </summary>
			[JsonProperty]
			public string? JobName { get; set; }

			/// <summary>
			/// Gets a value indicating whether IsPinOn
			/// Gets a value indicating the pin current state. <see cref="PinState"/>
			/// </summary>
			[JsonProperty]
			public bool IsPinOn => PinState == GpioPinState.On;

			/// <summary>
			/// Initializes a new instance of the <see cref="Pin"/> class.
			/// </summary>
			/// <param name="pin">The pin <see cref="int"/></param>
			/// <param name="state">The state <see cref="GpioPinState"/></param>
			/// <param name="mode">The mode <see cref="GpioPinMode"/></param>
			/// <param name="available">The status if the pin is currently available <see cref="bool"/></param>
			/// <param name="jobName">The jobName of the scheduler if the pin isn't available atm <see cref="string?"/></param>
			public Pin(int pin, GpioPinState state, GpioPinMode mode, bool available = true, string? jobName = null) {
				PinNumber = pin;
				PinState = state;
				Mode = mode;
				IsAvailable = available;
				JobName = jobName;
			}

			public Pin(int pin, GpioPinMode mode, bool available = true, string? jobName = null) {
				PinNumber = pin;				
				Mode = mode;
				IsAvailable = available;
				JobName = jobName;
			}

			public Pin(int pin, GpioPinState state, bool available = true, string? jobName = null) {
				PinNumber = pin;
				PinState = state;
				IsAvailable = available;
				JobName = jobName;
			}

			public Pin(int pin, bool available = true, string? jobName = null) {
				PinNumber = pin;
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
				s.AppendLine($"Pin Value -> {PinState.ToString()}");
				s.AppendLine($"Pin Mode -> {Mode.ToString()}");
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
			public override int GetHashCode() {
				return base.GetHashCode();
			}
		}

		/// <summary>
		/// Defines the PinConfigs
		/// </summary>
		[JsonProperty]
		public readonly List<Pin> PinConfigs = new List<Pin>(26);

		/// <summary>
		/// Initializes a new instance of the <see cref="PinConfig"/> class.
		/// </summary>
		/// <param name="configs">The configs<see cref="List{Pin}"/></param>
		public PinConfig(List<Pin> configs) {
			PinConfigs = configs;
		}
	}
}
