namespace Assistant.Gpio {
	public static class Enums {
		public enum GpioDriver {
			RaspberryIODriver,
			SystemDevicesDriver,
			WiringPiDriver
		}

		public enum PiAudioState {
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

		internal enum PinEventStates : byte {
			Activated,
			Deactivated,
			Both
		}

		///
		/// Summary:
		///  Different numbering schemes supported by GPIO controllers and drivers.
		public enum NumberingScheme {
			///
			/// Summary:
			/// The logical representation of the GPIOs. Refer to the microcontroller's datasheet
			/// to find this information.
			Logical,
			///
			/// Summary:
			/// The physical pin numbering that is usually accessible by the board headers.
			/// 
			Board
		}
	}
}
