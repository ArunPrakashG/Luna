using System;
using System.Runtime.InteropServices;
using Unosquare.RaspberryIO;

namespace Assistant.Gpio {
	internal static class GpioHelpers {
		internal static bool IsPiEnvironment() => Extensions.Helpers.GetOsPlatform() == OSPlatform.Linux && Pi.Info.RaspberryPiVersion.ToString().Equals("Pi3ModelBEmbest", StringComparison.OrdinalIgnoreCase);
	}
}
