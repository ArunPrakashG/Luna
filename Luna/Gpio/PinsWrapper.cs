namespace Luna.Gpio {
	internal struct PinsWrapper {
		internal readonly int[] OutputPins;
		internal readonly int[] InputPins;
		internal readonly int[] GpioPins;
		internal readonly int[] RelayPins;
		internal readonly int[] IrSensorPins;
		internal readonly int[] SoundSensorPins;

		internal PinsWrapper(int[] output, int[] input, int[] gpio, int[] relayPins, int[] irSensorPins, int[] soundSensorPins) {
			OutputPins = output;
			InputPins = input;
			GpioPins = gpio;
			RelayPins = relayPins;
			IrSensorPins = irSensorPins;
			SoundSensorPins = soundSensorPins;
		}
	}
}
