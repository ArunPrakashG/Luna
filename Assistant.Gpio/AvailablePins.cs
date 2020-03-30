namespace Assistant.Gpio {
	public struct AvailablePins {
		public readonly int[] OutputPins;
		public readonly int[] InputPins;
		public readonly int[] GpioPins;
		public readonly int[] RelayPins;
		public readonly int[] IrSensorPins;
		public readonly int[] SoundSensorPins;

		public AvailablePins(int[] output, int[] input, int[] gpio, int[] relayPins, int[] irSensorPins, int[] soundSensorPins) {
			OutputPins = output;
			InputPins = input;
			GpioPins = gpio;
			RelayPins = relayPins;
			IrSensorPins = irSensorPins;
			SoundSensorPins = soundSensorPins;
		}
	}
}
