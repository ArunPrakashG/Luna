using HomeAssistant.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Core {

	//High = OFF
	//Low = ON
	public class GPIOController {
		private string[] StartupArgs = null;
		private Logger Logger = new Logger("GPIO-CONTROLLER");
		private readonly GPIOConfigHandler GPIOConfigHandler;
		public List<GPIOPinConfig> GPIOConfig;
		public GPIOConfigRoot GPIOConfigRoot;

		public GPIOController(string[] startupargs, GPIOConfigRoot rootObject, List<GPIOPinConfig> config, GPIOConfigHandler configHandler) {
			StartupArgs = startupargs;
			GPIOConfig = config;
			GPIOConfigHandler = configHandler;
			GPIOConfigRoot = rootObject;
			Logger.Log("Initiated GPIO Controller class!");
		}

		private bool CheckSafeMode() {
			if (Program.Config.GPIOSafeMode) {
				return true;
			}

			if (!StartupArgs.Any() || StartupArgs == null || StartupArgs.Count() <= 0) {
				return false;
			}

			string arg = StartupArgs[0].Split('-')[1].Trim();

			if (string.IsNullOrEmpty(arg) || string.IsNullOrWhiteSpace(arg)) {
				return false;
			}

			if (arg.Equals("safe", StringComparison.OrdinalIgnoreCase)) {
				return true;
			}
			return false;
		}

		public GPIOPinConfig FetchPinStatus(int pin) {
			GPIOPinConfig Status = new GPIOPinConfig();

			foreach (GPIOPinConfig value in GPIOConfig) {
				if (value.Pin.Equals(pin)) {
					Status.IsOn = value.IsOn;
					Status.Mode = value.Mode;
					Status.Pin = value.Pin;
					return Status;
				}
			}

			return Status;
		}

		public GPIOPinConfig FetchPinStatus(BcmPin pin) {
			GPIOPinConfig Status = new GPIOPinConfig();

			foreach (GPIOPinConfig value in GPIOConfig) {
				if (value.Pin.Equals(pin)) {
					Status.IsOn = value.IsOn;
					Status.Mode = value.Mode;
					Status.Pin = value.Pin;
					return Status;
				}
			}

			return Status;
		}

		public bool SetGPIO(int pin, GpioPinDriveMode mode, GpioPinValue state) {
			if (!Program.Config.GPIOControl) {
				return false;
			}

			if (CheckSafeMode()) {
				if (!Program.Config.RelayPins.Contains(pin)) {
					Logger.Log($"Could not configure {pin} as safe mode is enabled.");
					return false;
				}
			}

			GPIOPinConfig Status = FetchPinStatus(pin);

			switch (state) {
				case GpioPinValue.High:
					if (Status.Pin.Equals(pin) && !Status.IsOn) {
						return true;
					}
					break;

				case GpioPinValue.Low:
					if (Status.Pin.Equals(pin) && Status.IsOn) {
						return true;
					}
					break;

				default:
					return false;
			}

			try {
				IGpioPin GPIOPin = Pi.Gpio[pin];
				GPIOPin.PinMode = mode;
				GPIOPin.Write(state);

				switch (mode) {
					case GpioPinDriveMode.Input:
						switch (state) {
							case GpioPinValue.High:
								Logger.Log($"Configured {pin} pin to OFF. (INPUT)");
								UpdatePinStatus(pin, false, PinMode.Input);
								break;

							case GpioPinValue.Low:
								Logger.Log($"Configured {pin} pin to ON. (INPUT)");
								UpdatePinStatus(pin, true, PinMode.Input);
								break;
						}
						break;

					case GpioPinDriveMode.Output:
						switch (state) {
							case GpioPinValue.High:
								Logger.Log($"Configured {pin} pin to OFF. (OUTPUT)");
								UpdatePinStatus(pin, false, PinMode.Output);
								break;

							case GpioPinValue.Low:
								Logger.Log($"Configured {pin} pin to ON. (OUTPUT)");
								UpdatePinStatus(pin, true, PinMode.Output);
								break;
						}
						break;

					default:
						goto case GpioPinDriveMode.Output;
				}

				return true;
			}
			catch (Exception e) {
				Logger.Log(e.ToString(), LogLevels.Error);
				return false;
			}
		}

		public bool SetGPIO(BcmPin pin, GpioPinDriveMode mode, GpioPinValue state) {
			if (!Program.Config.GPIOControl) {
				return false;
			}

			if (CheckSafeMode()) {
				if (!Program.Config.RelayPins.Contains((int) pin)) {
					Logger.Log("Could not configure {pin} as safe mode is enabled.");
					return false;
				}
			}

			GPIOPinConfig Status = FetchPinStatus(pin);

			switch (state) {
				case GpioPinValue.High:
					if (Status.Pin.Equals(pin) && !Status.IsOn) {
						return true;
					}
					break;

				case GpioPinValue.Low:
					if (Status.Pin.Equals(pin) && Status.IsOn) {
						return true;
					}
					break;

				default:
					return false;
			}

			try {
				IGpioPin GPIOPin = Pi.Gpio[pin];
				GPIOPin.PinMode = mode;
				GPIOPin.Write(state);

				switch (mode) {
					case GpioPinDriveMode.Input:
						switch (state) {
							case GpioPinValue.High:
								Logger.Log($"Configured {pin.ToString()} pin to OFF. (INPUT)");
								UpdatePinStatus(pin, false, PinMode.Input);
								break;

							case GpioPinValue.Low:
								Logger.Log($"Configured {pin.ToString()} pin to ON. (INPUT)");
								UpdatePinStatus(pin, true, PinMode.Input);
								break;
						}
						break;

					case GpioPinDriveMode.Output:
						switch (state) {
							case GpioPinValue.High:
								Logger.Log($"Configured {pin.ToString()} pin to OFF. (OUTPUT)");
								UpdatePinStatus(pin, false, PinMode.Output);
								break;

							case GpioPinValue.Low:
								Logger.Log($"Configured {pin.ToString()} pin to ON. (OUTPUT)");
								UpdatePinStatus(pin, true, PinMode.Output);
								break;
						}
						break;

					default:
						goto case GpioPinDriveMode.Output;
				}

				return true;
			}
			catch (Exception e) {
				Logger.Log(e.ToString(), LogLevels.Error);
				return false;
			}
		}

		private void UpdatePinStatus(int pin, bool IsOn, PinMode mode = PinMode.Output) {
			foreach (GPIOPinConfig value in GPIOConfig) {
				if (value.Pin.Equals(pin)) {
					value.Pin = pin;
					value.IsOn = IsOn;
					value.Mode = mode;
				}
			}
		}

		private void UpdatePinStatus(BcmPin pin, bool isOn, PinMode mode = PinMode.Output) {
			foreach (GPIOPinConfig value in GPIOConfig) {
				if (value.Pin.Equals(pin)) {
					value.Pin = (int) pin;
					value.IsOn = isOn;
					value.Mode = mode;
				}
			}
		}

		public async Task InitShutdown() {
			if (!GPIOConfig.Any() || GPIOConfig == null) {
				return;
			}

			await Task.Delay(100).ConfigureAwait(false);

			if (Program.Config.CloseRelayOnShutdown) {
				foreach (GPIOPinConfig value in GPIOConfig) {
					if (value.IsOn) {
						SetGPIO(value.Pin, GpioPinDriveMode.Output, GpioPinValue.High);
					}
				}
			}

			GPIOConfigRoot.GPIOData = GPIOConfig;
			GPIOConfigHandler.SaveGPIOConfig(GPIOConfigRoot);
		}

		public void DisplayPiInfo() {
			Logger.Log($"OS: {Pi.Info.OperatingSystem.SysName}", LogLevels.Trace);
			Logger.Log($"Processor count: {Pi.Info.ProcessorCount}", LogLevels.Trace);
			Logger.Log($"Model name: {Pi.Info.ModelName}", LogLevels.Trace);
			Logger.Log($"Release name: {Pi.Info.OperatingSystem.Release}", LogLevels.Trace);
			Logger.Log($"Board revision: {Pi.Info.BoardRevision}", LogLevels.Trace);
			Logger.Log($"Pi Version: {Pi.Info.RaspberryPiVersion.ToString()}", LogLevels.Trace);
			Logger.Log($"Memory size: {Pi.Info.MemorySize.ToString()}", LogLevels.Trace);
			Logger.Log($"Serial: {Pi.Info.Serial}", LogLevels.Trace);
			Logger.Log($"Pi Uptime: {Pi.Info.UptimeTimeSpan.TotalMinutes} minutes");
		}

		public void SetPiAudioState(PiAudioState state) {
			switch (state) {
				case PiAudioState.Mute:
					Pi.Audio.ToggleMute(true);
					break;

				case PiAudioState.Unmute:
					Pi.Audio.ToggleMute(false);
					break;
			}
		}

		public async Task SetPiVolume(int level = 80) {
			await Pi.Audio.SetVolumePercentage(level).ConfigureAwait(false);
		}

		public async Task SetPiVolume(float decibels = -1.00f) {
			await Pi.Audio.SetVolumeByDecibels(decibels).ConfigureAwait(false);
		}

		public async Task<bool> RelayTestService(GPIOCycles selectedCycle, int singleChannelValue = 0) {
			Logger.Log("Relay test service started!");

			switch (selectedCycle) {
				case GPIOCycles.OneTwo:
					_ = await RelayOneTwo().ConfigureAwait(false);
					break;

				case GPIOCycles.OneOne:
					_ = await RelayOneOne().ConfigureAwait(false);
					break;

				case GPIOCycles.OneMany:
					_ = await RelayOneMany().ConfigureAwait(false);
					break;

				case GPIOCycles.Cycle:
					_ = await RelayOneTwo().ConfigureAwait(false);
					_ = await RelayOneOne().ConfigureAwait(false);
					_ = await RelayOneMany().ConfigureAwait(false);
					break;

				case GPIOCycles.Single:
					_ = await RelaySingle(singleChannelValue, 8000).ConfigureAwait(false);
					break;

				case GPIOCycles.Base:
					Logger.Log("Base argument specified, running default cycle test!");
					goto case GPIOCycles.Cycle;
				case GPIOCycles.Default:
					Logger.Log("Unknown value, Aborting...");
					break;
			}
			return true;
		}

		public async Task<bool> RelaySingle(int pin = 0, int delayInMs = 8000) {
			SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.Low);
			Logger.Log($"Waiting for {delayInMs} ms to close the relay...");
			await Task.Delay(delayInMs).ConfigureAwait(false);
			SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.High);
			Logger.Log("Relay closed!");
			return true;
		}

		public async Task<bool> RelayOneTwo() {
			//make sure all relay is off
			foreach (int pin in Program.Config.RelayPins) {
				foreach (GPIOPinConfig pinvalue in GPIOConfig) {
					if (pin.Equals(pinvalue.Pin) && pinvalue.IsOn) {
						SetGPIO(pinvalue.Pin, GpioPinDriveMode.Output, GpioPinValue.High);
					}
				}
			}

			foreach (int pin in Program.Config.RelayPins) {
				Task.Delay(400).Wait();
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.Low);
			}

			await Task.Delay(500).ConfigureAwait(false);

			foreach (int pin in Program.Config.RelayPins) {
				Task.Delay(200).Wait();
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.High);
			}

			Task.Delay(800).Wait();

			foreach (int pin in Program.Config.RelayPins) {
				Task.Delay(200).Wait();
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.Low);
			}

			await Task.Delay(500).ConfigureAwait(false);

			foreach (int pin in Program.Config.RelayPins) {
				Task.Delay(400).Wait();
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.High);
			}
			return true;
		}

		public async Task<bool> RelayOneOne() {
			//make sure all relay is off
			foreach (int pin in Program.Config.RelayPins) {
				foreach (GPIOPinConfig pinvalue in GPIOConfig) {
					if (pin.Equals(pinvalue.Pin) && pinvalue.IsOn) {
						SetGPIO(pinvalue.Pin, GpioPinDriveMode.Output, GpioPinValue.High);
					}
				}
			}

			foreach (int pin in Program.Config.RelayPins) {
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.Low);
				Task.Delay(500).Wait();
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.High);
				await Task.Delay(100).ConfigureAwait(false);
			}

			return true;
		}

		public async Task<bool> RelayOneMany() {
			//make sure all relay is off
			foreach (int pin in Program.Config.RelayPins) {
				foreach (GPIOPinConfig pinvalue in GPIOConfig) {
					if (pin.Equals(pinvalue.Pin) && pinvalue.IsOn) {
						SetGPIO(pinvalue.Pin, GpioPinDriveMode.Output, GpioPinValue.High);
					}
				}
			}

			int counter = 0;

			foreach (int pin in Program.Config.RelayPins) {
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.Low);

				while (counter < 4) {
					Task.Delay(200).Wait();
					SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.High);
					Task.Delay(500).Wait();
					SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.Low);
					counter++;
				}

				await Task.Delay(100).ConfigureAwait(false);
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.High);
			}

			return true;
		}
	}
}
