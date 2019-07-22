using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Devices.Gpio;
using System.Linq;
using System.Threading.Tasks;
using Assistant.Extensions;
using Assistant.Log;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;

namespace Assistant.AssistantCore {

	public class GPIOTaskQueue {

		public int PinNumber { get; set; }

		public DateTime CreationTime { get; set; }

		public DateTime EndingTime { get; set; }

		public GpioPinValue InitialValue { get; set; }

		public GpioPinValue FinalValue { get; set; }

		public string Message { get; set; }

		public TimeSpan Delay { get; set; }
	}

	//High = OFF
	//Low = ON
	public class GPIOController {
		private readonly Logger Logger = new Logger("GPIO-CONTROLLER");
		private readonly GPIOConfigHandler GPIOConfigHandler;

		public List<GPIOPinConfig> GPIOConfig { get; set; }

		public GPIOConfigRoot GPIOConfigRoot { get; private set; }

		private bool IsWaitForValueCancellationRequested { get; set; } = false;

		public GpioEventManager GpioPollingManager { get; private set; }

		private readonly ConcurrentQueue<GPIOTaskQueue> TaskQueue = new ConcurrentQueue<GPIOTaskQueue>();

		public GPIOController(GPIOConfigRoot rootObject, List<GPIOPinConfig> config, GPIOConfigHandler configHandler) {
			GPIOConfig = config;
			GPIOConfigHandler = configHandler;
			GPIOConfigRoot = rootObject;
			GpioPollingManager = new GpioEventManager(this);

			Helpers.ScheduleTask(() => {
				if (GpioPollingManager == null) {
					return;
				}

				List<GpioPinEventData> pinData = new List<GpioPinEventData>();

				foreach (int pin in Core.Config.RelayPins) {
					pinData.Add(new GpioPinEventData() {
						PinMode = GpioPinDriveMode.Output,
						GpioPin = pin,
						PinEventState = Enums.GpioPinEventStates.ALL
					});
				}

				GpioPollingManager.RegisterGpioEvent(pinData);

				foreach (GpioEventGenerator c in GpioPollingManager.GpioPinEventGenerators) {
					c.GPIOPinValueChanged += OnGpioPinValueChanged;
				}
			}, TimeSpan.FromSeconds(5));

			Logger.Log("Initiated GPIO Controller class!", Enums.LogLevels.Trace);
		}

		private void OnGpioPinValueChanged(object sender, GpioPinValueChangedEventArgs e) {
			switch (e.PinState) {
				case Enums.GpioPinEventStates.OFF when e.PinPreviousState == Enums.GpioPinEventStates.OFF:
					Logger.Log($"{e.PinNumber} gpio pin set to OFF state. (OFF)");
					break;

				case Enums.GpioPinEventStates.ON when e.PinPreviousState == Enums.GpioPinEventStates.ON:
					Logger.Log($"{e.PinNumber} gpio pin set to ON state. (ON)");
					break;

				case Enums.GpioPinEventStates.ON when e.PinPreviousState == Enums.GpioPinEventStates.OFF:
					Logger.Log($"{e.PinNumber} gpio pin set to ON state. (OFF)");
					break;

				case Enums.GpioPinEventStates.OFF when e.PinPreviousState == Enums.GpioPinEventStates.ON:
					Logger.Log($"{e.PinNumber} gpio pin set to OFF state. (ON)");
					break;

				default:
					Logger.Log($"Value for {e.PinNumber} pin changed to {e.PinCurrentDigitalValue} from {e.PinPreviousDigitalValue.ToString()}");
					break;
			}
		}

		private void OnEnqueued(GPIOTaskQueue item) {
		}

		private void OnDequeued(GPIOTaskQueue item) {
		}

		private void TryEnqueue(GPIOTaskQueue task) {
			if (task == null) {
				Logger.Log("Task is null.", Enums.LogLevels.Warn);
				return;
			}

			TaskQueue.Enqueue(task);
			Helpers.InBackgroundThread(() => OnEnqueued(task), "OnEnqueued");
			Logger.Log("Task added sucessfully.", Enums.LogLevels.Trace);
		}

		private GPIOTaskQueue TryDequeue() {
			bool result = TaskQueue.TryDequeue(out GPIOTaskQueue task);

			if (!result) {
				Logger.Log("Failed to fetch from the queue.", Enums.LogLevels.Error);
				return null;
			}

			Logger.Log("Fetching task sucessfully!", Enums.LogLevels.Trace);
			Helpers.InBackground(() => OnDequeued(task));
			return task;
		}

		[Obsolete("Temporary use for testing only")]
		public void GpioTest() {
			GpioController controllerTest = new GpioController(PinNumberingScheme.Gpio);
			if (!controllerTest.IsPinOpen(Core.Config.RelayPins.FirstOrDefault())) {
				controllerTest.OpenPin(Core.Config.RelayPins.FirstOrDefault());
			}

			controllerTest[Core.Config.RelayPins.FirstOrDefault()].Mode = PinMode.Output;
			controllerTest[Core.Config.RelayPins.FirstOrDefault()].NotifyEvents = PinEvent.Any;
			controllerTest[Core.Config.RelayPins.FirstOrDefault()].EnableRaisingEvents = true;
			controllerTest[Core.Config.RelayPins.FirstOrDefault()].ValueChanged += OnPinValueChangedTest;
		}

		private void OnPinValueChangedTest(object sender, PinValueChangedEventArgs e) {
			Logger.Log($"pin value changed test method fired for pin {e.GpioPinNumber}");
		}

		private bool CheckSafeMode() => Core.Config.GPIOSafeMode;

		public void StopWaitForValue() => IsWaitForValueCancellationRequested = true;

		public void ChargerController(int pin, TimeSpan delay, GpioPinValue initialValue, GpioPinValue finalValue) {
			if (pin <= 0) {
				Logger.Log("Pin number is set to unknown value.", Enums.LogLevels.Error);
				return;
			}

			if (Core.Config.IRSensorPins.Contains(pin)) {
				Logger.Log("Sorry, the specified pin is pre-configured for IR Sensor. cannot modify!", Enums.LogLevels.Error);
				return;
			}

			if (!Core.Config.RelayPins.Contains(pin)) {
				Logger.Log("Sorry, the specified pin doesn't exist in the relay pin catagory.", Enums.LogLevels.Error);
				return;
			}

			GPIOPinConfig PinStatus = Core.Controller.FetchPinStatus(pin);

			//TODO: Task based system for scheduling various tasks like remainders and charger controller
			if (initialValue == GpioPinValue.High && finalValue == GpioPinValue.Low) {
				if (PinStatus.IsOn) {
					GPIOTaskQueue task = new GPIOTaskQueue() {
						CreationTime = DateTime.Now,
						EndingTime = DateTime.Now.Add(delay),
						Delay = delay,
						Message = null,
						FinalValue = finalValue,
						InitialValue = initialValue,
						PinNumber = pin
					};
					TryEnqueue(task);
					Core.Controller.SetGPIO(pin, GpioPinDriveMode.Output, initialValue);
					Logger.Log($"TASK >> {pin} configured to OFF state. Waiting {delay.Minutes} minutes...");
					Core.Controller.FetchPinStatus(pin);
					Helpers.ScheduleTask(() => {
						Core.Controller.SetGPIO(pin, GpioPinDriveMode.Output, finalValue);
						Logger.Log($"TASK >> {pin} configured to ON state. Task completed sucessfully!");
					}, delay);
				}
				else {
					Logger.Log("Pin is already in OFF state. disposing the task.", Enums.LogLevels.Error);
				}
			}
			else if (initialValue == GpioPinValue.Low && finalValue == GpioPinValue.High) {
				if (!PinStatus.IsOn) {
					GPIOTaskQueue task = new GPIOTaskQueue() {
						CreationTime = DateTime.Now,
						EndingTime = DateTime.Now.Add(delay),
						Delay = delay,
						Message = null,
						FinalValue = finalValue,
						InitialValue = initialValue,
						PinNumber = pin
					};
					TryEnqueue(task);
					Core.Controller.SetGPIO(pin, GpioPinDriveMode.Output, initialValue);
					Logger.Log($"TASK >> {pin} configured to ON state. Waiting {delay.Minutes} minutes...");
					Core.Controller.FetchPinStatus(pin);
					Helpers.ScheduleTask(() => {
						Core.Controller.SetGPIO(pin, GpioPinDriveMode.Output, finalValue);
						Logger.Log($"TASK >> {pin} configured to OFF state. Task completed sucessfully!");
					}, delay);
				}
				else {
					Logger.Log("Pin is already in ON state. disposing the task.", Enums.LogLevels.Error);
				}
			}
			else if (initialValue == GpioPinValue.Low && finalValue == GpioPinValue.Low) {
				Logger.Log("Both initial and final values cant be equal. (ON-ON)", Enums.LogLevels.Error);
			}
			else if (initialValue == GpioPinValue.High && finalValue == GpioPinValue.High) {
				Logger.Log("Both initial and final values cant be equal (OFF-OFF)", Enums.LogLevels.Error);
			}
			else {
				Logger.Log("Unknown value, an error has occured.", Enums.LogLevels.Error);
			}
		}

		public void ContinuousWaitForValue(int pin, bool pinValue, Action action) {
			if (action == null) {
				Logger.Log("Action is null!", Enums.LogLevels.Error);
				return;
			}

			if (pin == 0) {
				Logger.Log("Pin number is set to unknown value.", Enums.LogLevels.Error);
				return;
			}

			if (!Core.Config.IRSensorPins.Contains(pin)) {
				Logger.Log("The specified pin doesn't exist in IR Sensor pin list.", Enums.LogLevels.Error);
				return;
			}

			int failedAttempts = 0;

			while (true) {
				if (IsWaitForValueCancellationRequested) {
					break;
				}

				if (failedAttempts.Equals(7)) {
					Logger.Log("Failed to run the specified action.", Enums.LogLevels.Error);
					return;
				}

				bool? pinState = GpioDigitalRead(pin);

				if (!pinState.HasValue) {
					Logger.Log("Could not fetch pin state value, trying again...", Enums.LogLevels.Trace);
					failedAttempts++;
					continue;
				}

				if (pinState.Equals(pinValue)) {
					Task.Run(action);
				}
				Task.Delay(100).Wait();
			}
		}

		public bool WaitUntilPinValue(int pin, GpioPinValue value, int timeOutValueMillisecond) {
			if (pin <= 0) {
				Logger.Log("Pin number is set to unknown value.", Enums.LogLevels.Error);
				return false;
			}

			if (timeOutValueMillisecond <= 0) {
				Logger.Log("Time out value is set to unknown digit.", Enums.LogLevels.Error);
			}

			IGpioPin GPIOPin = Pi.Gpio[pin];
			return GPIOPin.WaitForValue(value, timeOutValueMillisecond);
		}

		public bool RunOnPinValue(int pin, bool pinValueConditon, Action action) {
			if (action == null) {
				Logger.Log("Action is null!", Enums.LogLevels.Error);
				return false;
			}

			if (pin == 0) {
				Logger.Log("Pin number is set to unknown value.", Enums.LogLevels.Error);
				return false;
			}

			int failedAttempts = 0;

			while (true) {
				if (failedAttempts.Equals(7)) {
					Logger.Log("Failed to run the specified action.", Enums.LogLevels.Error);
					return false;
				}

				bool? pinState = GpioDigitalRead(pin);

				if (!pinState.HasValue) {
					Logger.Log("Could not fetch pin state value, trying again...", Enums.LogLevels.Trace);
					failedAttempts++;
					continue;
				}

				if (pinState.Value.Equals(pinValueConditon)) {
					Helpers.InBackground(action);
					Logger.Log("Started the specified action process.", Enums.LogLevels.Trace);
					return true;
				}
				Task.Delay(100).Wait();
			}
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

		public bool GpioDigitalRead(int pin, bool onlyInputPins = false) {
			IGpioPin GPIOPin = Pi.Gpio[pin];

			if (onlyInputPins && GPIOPin.PinMode != GpioPinDriveMode.Input) {
				Logger.Log("The specified gpio pin mode isn't set to Input.", Enums.LogLevels.Error);
				return false;
			}

			return GPIOPin.Read();
		}

		public bool SetGPIO(int pin, GpioPinDriveMode mode, GpioPinValue state) {
			if (!Core.Config.EnableGpioControl) {
				return false;
			}

			if (CheckSafeMode()) {
				if (!Core.Config.RelayPins.Contains(pin)) {
					Logger.Log($"Could not configure {pin} as it's marked as SAFE. (SAFE-MODE)");
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

			IGpioPin GPIOPin = Pi.Gpio[pin];
			GPIOPin.PinMode = mode;
			GPIOPin.Write(state);

			switch (mode) {
				case GpioPinDriveMode.Input:
					switch (state) {
						case GpioPinValue.High:
							Logger.Log($"Configured {pin} pin to OFF. (INPUT)", Enums.LogLevels.Trace);
							UpdatePinStatus(pin, false, Enums.PinMode.Input);
							break;

						case GpioPinValue.Low:
							Logger.Log($"Configured {pin} pin to ON. (INPUT)", Enums.LogLevels.Trace);
							UpdatePinStatus(pin, true, Enums.PinMode.Input);
							break;
					}
					break;

				case GpioPinDriveMode.Output:
					switch (state) {
						case GpioPinValue.High:
							Logger.Log($"Configured {pin} pin to OFF. (OUTPUT)", Enums.LogLevels.Trace);
							UpdatePinStatus(pin, false);
							break;

						case GpioPinValue.Low:
							Logger.Log($"Configured {pin} pin to ON. (OUTPUT)", Enums.LogLevels.Trace);
							UpdatePinStatus(pin, true);
							break;
					}
					break;

				default:
					goto case GpioPinDriveMode.Output;
			}

			return true;
		}

		public bool SetGPIO(BcmPin pin, GpioPinDriveMode mode, GpioPinValue state) {
			if (!Core.Config.EnableGpioControl) {
				return false;
			}

			if (CheckSafeMode()) {
				if (!Core.Config.RelayPins.Contains((int) pin)) {
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
								Logger.Log($"Configured {pin.ToString()} pin to OFF. (INPUT)", Enums.LogLevels.Trace);
								UpdatePinStatus(pin, false, Enums.PinMode.Input);
								break;

							case GpioPinValue.Low:
								Logger.Log($"Configured {pin.ToString()} pin to ON. (INPUT)", Enums.LogLevels.Trace);
								UpdatePinStatus(pin, true, Enums.PinMode.Input);
								break;
						}
						break;

					case GpioPinDriveMode.Output:
						switch (state) {
							case GpioPinValue.High:
								Logger.Log($"Configured {pin.ToString()} pin to OFF. (OUTPUT)", Enums.LogLevels.Trace);
								UpdatePinStatus(pin, false);
								break;

							case GpioPinValue.Low:
								Logger.Log($"Configured {pin.ToString()} pin to ON. (OUTPUT)", Enums.LogLevels.Trace);
								UpdatePinStatus(pin, true);
								break;
						}
						break;

					default:
						goto case GpioPinDriveMode.Output;
				}

				return true;
			}
			catch (Exception e) {
				Logger.Log(e.ToString(), Enums.LogLevels.Error);
				return false;
			}
		}

		private void UpdatePinStatus(int pin, bool IsOn, Enums.PinMode mode = Enums.PinMode.Output) {
			foreach (GPIOPinConfig value in GPIOConfig) {
				if (value.Pin.Equals(pin)) {
					value.Pin = pin;
					value.IsOn = IsOn;
					value.Mode = mode;
				}
			}
		}

		private void UpdatePinStatus(BcmPin pin, bool isOn, Enums.PinMode mode = Enums.PinMode.Output) {
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

			if (GpioPollingManager != null) {
				foreach (GpioEventGenerator c in GpioPollingManager.GpioPinEventGenerators) {
					c.GPIOPinValueChanged -= OnGpioPinValueChanged;
				}
			}

			GpioPollingManager.ExitEventGenerator();

			if (Core.Config.CloseRelayOnShutdown) {
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
			Logger.Log($"OS: {Pi.Info.OperatingSystem.SysName}", Enums.LogLevels.Trace);
			Logger.Log($"Processor count: {Pi.Info.ProcessorCount}", Enums.LogLevels.Trace);
			Logger.Log($"Model name: {Pi.Info.ModelName}", Enums.LogLevels.Trace);
			Logger.Log($"Release name: {Pi.Info.OperatingSystem.Release}", Enums.LogLevels.Trace);
			Logger.Log($"Board revision: {Pi.Info.BoardRevision}", Enums.LogLevels.Trace);
			Logger.Log($"Pi Version: {Pi.Info.RaspberryPiVersion.ToString()}", Enums.LogLevels.Trace);
			Logger.Log($"Memory size: {Pi.Info.MemorySize.ToString()}", Enums.LogLevels.Trace);
			Logger.Log($"Serial: {Pi.Info.Serial}", Enums.LogLevels.Trace);
			Logger.Log($"Pi Uptime: {Math.Round(Pi.Info.UptimeTimeSpan.TotalMinutes, 4)} minutes", Enums.LogLevels.Trace);
		}

		public void SetPiAudioState(Enums.PiAudioState state) {
			switch (state) {
				case Enums.PiAudioState.Mute:
					Pi.Audio.ToggleMute(true);
					break;

				case Enums.PiAudioState.Unmute:
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

		public async Task<bool> RelayTestService(Enums.GPIOCycles selectedCycle, int singleChannelValue = 0) {
			Logger.Log("Relay test service started!");

			switch (selectedCycle) {
				case Enums.GPIOCycles.OneTwo:
					_ = await RelayOneTwo().ConfigureAwait(false);
					break;

				case Enums.GPIOCycles.OneOne:
					_ = await RelayOneOne().ConfigureAwait(false);
					break;

				case Enums.GPIOCycles.OneMany:
					_ = await RelayOneMany().ConfigureAwait(false);
					break;

				case Enums.GPIOCycles.Cycle:
					_ = await RelayOneTwo().ConfigureAwait(false);
					_ = await RelayOneOne().ConfigureAwait(false);
					_ = await RelayOneMany().ConfigureAwait(false);
					break;

				case Enums.GPIOCycles.Single:
					_ = await RelaySingle(singleChannelValue, 8000).ConfigureAwait(false);
					break;

				case Enums.GPIOCycles.Base:
					Logger.Log("Base argument specified, running default cycle test!");
					goto case Enums.GPIOCycles.Cycle;
				case Enums.GPIOCycles.Default:
					Logger.Log("Unknown value, Aborting...");
					break;
			}
			return true;
		}

		public async Task<bool> RelaySingle(int pin = 0, int delayInMs = 8000) {
			SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.Low);
			Logger.Log($"Waiting for {delayInMs} ms to close the relay...");
			await System.Threading.Tasks.Task.Delay(delayInMs).ConfigureAwait(false);
			SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.High);
			Logger.Log("Relay closed!");
			return true;
		}

		public async Task<bool> RelayOneTwo() {

			//make sure all relay is off
			foreach (int pin in Core.Config.RelayPins) {
				foreach (GPIOPinConfig pinvalue in GPIOConfig) {
					if (pin.Equals(pinvalue.Pin) && pinvalue.IsOn) {
						SetGPIO(pinvalue.Pin, GpioPinDriveMode.Output, GpioPinValue.High);
					}
				}
			}

			foreach (int pin in Core.Config.RelayPins) {
				Task.Delay(400).Wait();
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.Low);
			}

			await Task.Delay(500).ConfigureAwait(false);

			foreach (int pin in Core.Config.RelayPins) {
				Task.Delay(200).Wait();
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.High);
			}

			Task.Delay(800).Wait();

			foreach (int pin in Core.Config.RelayPins) {
				Task.Delay(200).Wait();
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.Low);
			}

			await Task.Delay(500).ConfigureAwait(false);

			foreach (int pin in Core.Config.RelayPins) {
				Task.Delay(400).Wait();
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.High);
			}
			return true;
		}

		public async Task<bool> RelayOneOne() {

			//make sure all relay is off
			foreach (int pin in Core.Config.RelayPins) {
				foreach (GPIOPinConfig pinvalue in GPIOConfig) {
					if (pin.Equals(pinvalue.Pin) && pinvalue.IsOn) {
						SetGPIO(pinvalue.Pin, GpioPinDriveMode.Output, GpioPinValue.High);
					}
				}
			}

			foreach (int pin in Core.Config.RelayPins) {
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.Low);
				Task.Delay(500).Wait();
				SetGPIO(pin, GpioPinDriveMode.Output, GpioPinValue.High);
				await Task.Delay(100).ConfigureAwait(false);
			}

			return true;
		}

		public async Task<bool> RelayOneMany() {

			//make sure all relay is off
			foreach (int pin in Core.Config.RelayPins) {
				foreach (GPIOPinConfig pinvalue in GPIOConfig) {
					if (pin.Equals(pinvalue.Pin) && pinvalue.IsOn) {
						SetGPIO(pinvalue.Pin, GpioPinDriveMode.Output, GpioPinValue.High);
					}
				}
			}

			int counter = 0;

			foreach (int pin in Core.Config.RelayPins) {
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
