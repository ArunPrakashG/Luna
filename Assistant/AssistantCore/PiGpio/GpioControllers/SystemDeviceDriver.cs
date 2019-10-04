using Assistant.Log;
using JetBrains.Annotations;
using System;
using System.Device.Gpio;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Assistant.AssistantCore.Enums;

namespace Assistant.AssistantCore.PiGpio.GpioControllers {
	internal class SystemDeviceDriver : IGpioControllerDriver {
		private readonly GpioPinController GpioController;
		private readonly Logger Logger;
		private GpioController? Controller { get; set; }

		public bool IsDriverProperlyInitialized { get; private set; }

		internal SystemDeviceDriver(GpioPinController gpioController) {
			GpioController = gpioController ?? throw new ArgumentNullException(nameof(gpioController), "The Gpio Controller cannot be null!");
			Logger = GpioController.Logger;
		}

		[CanBeNull]
		internal SystemDeviceDriver? InitDriver(PinNumberingScheme numberingScheme) {
			if (Core.DisablePiMethods || Core.RunningPlatform != OSPlatform.Linux || !Core.Config.EnableGpioControl) {
				Logger.Log("Failed to initialize Gpio Controller Driver.", LogLevels.Warn);
				IsDriverProperlyInitialized = false;
				return null;
			}

			Controller = new GpioController(numberingScheme);
			IsDriverProperlyInitialized = true;
			return this;
		}

		[CanBeNull]
		public GpioPinConfig GetGpioConfig(int pinNumber) {
			if (!PiController.IsValidPin(pinNumber) || Controller == null) {
				return new GpioPinConfig();
			}

			PinValue value = Controller.Read(pinNumber);
			PinMode mode = Controller.GetPinMode(pinNumber);
			GpioPinConfig config = new GpioPinConfig(pinNumber, value == PinValue.High ? GpioPinState.Off : GpioPinState.On, mode == PinMode.Input ? GpioPinMode.Input : GpioPinMode.Output, false, 0);
			return config;
		}

		public bool GpioDigitalRead(int pin) {
			throw new NotImplementedException();
		}

		public int GpioPhysicalPinNumber(int bcmPin) {
			throw new NotImplementedException();
		}

		public Enums.GpioPinState GpioPinStateRead(int pin) {
			throw new NotImplementedException();
		}

		public Task<bool> RelayTestServiceAsync(Enums.GpioCycles selectedCycle, int singleChannelValue = 0) {
			throw new NotImplementedException();
		}

		public bool SetGpioValue(int pin, Enums.GpioPinMode mode) {
			throw new NotImplementedException();
		}

		public bool SetGpioValue(int pin, Enums.GpioPinMode mode, Enums.GpioPinState state) {
			throw new NotImplementedException();
		}

		public bool SetGpioValue(int pin, Enums.GpioPinState state) {
			throw new NotImplementedException();
		}

		public bool SetGpioWithTimeout(int pin, Enums.GpioPinMode mode, Enums.GpioPinState state, TimeSpan duration) {
			throw new NotImplementedException();
		}

		public void ShutdownDriver() {
			throw new NotImplementedException();
		}

		public void UpdatePinConfig(int pin, Enums.GpioPinMode mode, Enums.GpioPinState value, TimeSpan duration) {
			throw new NotImplementedException();
		}
	}
}
