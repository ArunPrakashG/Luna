namespace Luna.Gpio {
	internal class Enums {
		internal enum GpioDriver {
			RaspberryIODriver,
			SystemDevicesDriver,
			WiringPiDriver
		}

		internal enum PiAudioState {
			Mute,
			Unmute
		}

		internal enum GpioPinMode {
			Input = 0,
			Output = 1,
			Alt01 = 4,
			Alt02 = 5
		}

		internal enum GpioCycles : byte {
			Cycle,
			Single,
			Base,
			OneMany,
			OneTwo,
			OneOne,
			Default
		}

		internal enum GpioPinState {
			On = 0,
			Off = 1
		}

		internal enum PinEventStates : byte {
			Activated,
			Deactivated,
			Both
		}

		/// <summary>
		///  Different numbering schemes supported by GPIO controllers and drivers.
		/// </summary>
		internal enum NumberingScheme {
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
