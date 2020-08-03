using Luna.Extensions;
using Luna.ExternalExtensions;
using Luna.Gpio.Controllers;
using Luna.Gpio.Drivers;
using Luna.Gpio.Exceptions;
using Luna.Gpio.PinEvents.PinEventArgs;
using Luna.Logging;
using Luna.TypeLoader;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static Luna.Gpio.Enums;

namespace Luna.Gpio.PinEvents {
	internal sealed class InternalEventGenerator : IDisposable {
		private const int POLL_DELAY = 1; // in ms		
		private readonly InternalLogger Logger = new InternalLogger(nameof(InternalEventGenerator));
		private readonly GpioControllerDriver Driver;
		private readonly Core Core;
		private readonly CancellationTokenSource ShutdownEventGenerationToken;
		private readonly bool IsPossible;
		private readonly List<IPinEvent> LoadablePinEvents = new List<IPinEvent>();
		private readonly List<PinEventConfiguration> EventConfigurations = new List<PinEventConfiguration>();

		internal InternalEventGenerator(Core core, GpioControllerDriver driver) {
			Core = core ?? throw new ArgumentNullException(nameof(core));
			Driver = driver ?? throw new ArgumentNullException(nameof(driver));
			ShutdownEventGenerationToken = new CancellationTokenSource();

			if (!Driver.IsDriverInitialized) {
				throw new DriverNotInitializedException();
			}

			SetInitialPinValues();
			Helpers.InBackgroundThread(async () => await LoadInternalPinEvents().ConfigureAwait(false));
		}

		internal void InitEventGeneration() {
			Core.Config.GpioConfiguration.InputModePins.ForEach((pin) => {
				Pin pinConfig = Driver.GetPinConfig(pin);
				CurrentValue currentValue = new CurrentValue(pinConfig.PinState, pinConfig.IsPinOn, pinConfig.Mode, PinEventState.Both);
				PreviousValue previousValue = new PreviousValue(pinConfig.PinState, pinConfig.IsPinOn, pinConfig.Mode, PinEventState.Both);
				PinEventConfiguration config = new PinEventConfiguration(pin, PinEventState.Both, currentValue, previousValue, new CancellationTokenSource());
				Helpers.InBackgroundThread(() => Generate(ref config), true);
				EventConfigurations.Add(config);
			});

			Core.Config.GpioConfiguration.OutputModePins.ForEach((pin) => {
				Pin pinConfig = Driver.GetPinConfig(pin);
				CurrentValue currentValue = new CurrentValue(pinConfig.PinState, pinConfig.IsPinOn, pinConfig.Mode, PinEventState.Both);
				PreviousValue previousValue = new PreviousValue(pinConfig.PinState, pinConfig.IsPinOn, pinConfig.Mode, PinEventState.Both);
				PinEventConfiguration config = new PinEventConfiguration(pin, PinEventState.Both, currentValue, previousValue, new CancellationTokenSource());
				Helpers.InBackgroundThread(() => Generate(ref config), true);
				EventConfigurations.Add(config);
			});

			Logger.Info($"'{EventConfigurations.Count}' pin configurations with events initiated.");
		}

		private async Task LoadInternalPinEvents() {
			await foreach (var p in InternalTypeLoader.LoadInternalTypes<IPinEvent>()) {
				LoadablePinEvents.Add(p);
				p.OnInitialized(this, new EventArgs());
			}
		}

		private void SetInitialPinValues() {
			Core.Config.GpioConfiguration.InputModePins.ForEach((p) => Driver.SetGpioValue(p, GpioPinMode.Input));
			Core.Config.GpioConfiguration.OutputModePins.ForEach((p) => Driver.SetGpioValue(p, GpioPinMode.Output, GpioPinState.Off));
			Logger.Trace($"Initial values configured for '{Core.Config.GpioConfiguration.InputModePins.Length + Core.Config.GpioConfiguration.OutputModePins.Length}' gpio pins.");
		}

		// Should be invoked on a different thread
		private void Generate(ref PinEventConfiguration config) {
			if (config == null || !PinController.IsValidPin(config.GpioPin)) {
				return;
			}

			config.IsEventRegistered = true;

			do {
				Pin pinConfig = Driver.GetPinConfig(config.GpioPin);
				config.Current = new CurrentValue(pinConfig.PinState, pinConfig.IsPinOn, pinConfig.Mode, config.EventState);
				OnEventResult(ref config);
				Thread.Sleep(POLL_DELAY);
				config.Previous = config.Current;
			} while (!ShutdownEventGenerationToken.IsCancellationRequested || !config.EventToken.IsCancellationRequested);

			config.IsEventRegistered = false;
			Logger.Trace($"Polling for '{config.GpioPin}' has been stopped.");
		}

		private void OnEventResult(ref PinEventConfiguration config) {
			if (config == null || !PinController.IsValidPin(config.GpioPin)) {
				return;
			}

			if (config.Current.Equals(config.Previous)) {
				return;
			}

			OnValueChangedEventArgs onValueChangedEventArgs = new OnValueChangedEventArgs(config.GpioPin, (CurrentValue) config.Current, (PreviousValue) config.Previous);

			switch (config.EventState) {
				case PinEventState.Activated when config.Current.DigitalValue:
					OnActivatedEventArgs onActivatedEventArgs = new OnActivatedEventArgs(config.GpioPin, (CurrentValue) config.Current);
					ParallelExecuteOnEach(config.EventState, (p) => p.OnActivated(this, onActivatedEventArgs)).RunSynchronously();
					break;
				case PinEventState.Deactivated when !config.Current.DigitalValue:
					OnDeactivatedEventArgs onDeactivatedEventArgs = new OnDeactivatedEventArgs(config.GpioPin, (CurrentValue) config.Current);
					ParallelExecuteOnEach(config.EventState, (p) => p.OnDeactivated(this, onDeactivatedEventArgs)).RunSynchronously();
					break;
				case PinEventState.Both:
					break;
			}

			ParallelExecuteOnEach(config.EventState, (p) => p.OnValueChanged(this, onValueChangedEventArgs)).RunSynchronously();
		}

		private async Task ParallelExecuteOnEach(PinEventState state, Action<IPinEvent> action) {
			var tasks = new List<Task>();
			for (int i = 0; i < LoadablePinEvents.Count; i++) {
				tasks.Add(new Task(() => action(LoadablePinEvents[i])));
			}

			await Helpers.InParallel(tasks).ConfigureAwait(false);
		}

		public void Dispose() {
			ShutdownEventGenerationToken?.Cancel();
			ShutdownEventGenerationToken?.Dispose();
		}
	}
}
