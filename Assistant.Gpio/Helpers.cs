using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Unosquare.RaspberryIO;

namespace Assistant.Gpio {
	internal static class Helpers {
		internal static bool IsPiEnvironment() => Extensions.Helpers.GetOsPlatform() == OSPlatform.Linux && Pi.Info.RaspberryPiVersion.ToString().Equals("Pi3ModelBEmbest", StringComparison.OrdinalIgnoreCase);
	}
}
