namespace Assistant.Gpio {
	public struct AvailablePins {
		public readonly int[] OutputPins;
		public readonly int[] InputPins;
		public readonly int[] GpioPins;

		public AvailablePins(int[] output, int[] input, int[] gpio) {
			OutputPins = output;
			InputPins = input;
			GpioPins = gpio;
		}
	}
}
