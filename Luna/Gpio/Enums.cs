namespace Luna.Gpio {
	public class Enums {
		public enum GpioDriver {
			RaspberryIODriver,
			SystemDevicesDriver,
			WiringPiDriver
		}

		public enum AudioState {
			Mute,
			Unmute
		}

		public enum GpioPinMode {
			Input = 0,
			Output = 1,
			Alt01 = 4,
			Alt02 = 5
		}

		public enum GpioCycles : byte {
			Cycle,
			Single,
			Base,
			OneMany,
			OneTwo,
			OneOne,
			Default
		}

		public enum GpioPinState {
			On = 0,
			Off = 1
		}

		public enum PinEventState : byte {
			Activated,
			Deactivated,
			Both
		}

		/// <summary>
		///  Different numbering schemes supported by GPIO controllers and drivers.
		/// </summary>
		public enum NumberingScheme {
			/// <summary>
			/// The logical representation of the GPIOs. Refer to the microcontroller's datasheet
			/// to find this information.
			/// </summary>
			Logical,

			/// <summary>
			/// The physical pin numbering that is usually accessible by the board headers.
			/// </summary>
			Board
		}
	}
}
