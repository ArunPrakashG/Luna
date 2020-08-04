using Luna.Gpio.PinEvents;
using Luna.Gpio.PinEvents.PinEventArgs;
using Luna.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Luna.Gpio {
	internal class PinEvent : IPinEvent {
		private readonly InternalLogger Logger = new InternalLogger(nameof(PinEvent));

		public void OnActivated(object sender, OnActivatedEventArgs e) {
			if((sender == null) || (e == null)) {
				return;
			}

			Logger.Warn($"[{nameof(OnActivated)}] | {e.TimeStamp} -> {e.Pin} -> {e.Current.State}");
		}

		public void OnDeactivated(object sender, OnDeactivatedEventArgs e) {
			if ((sender == null) || (e == null)) {
				return;
			}

			Logger.Warn($"[{nameof(OnDeactivated)}] | {e.TimeStamp} -> {e.Pin} -> {e.Current.State}");
		}

		public void OnInitialized(object sender, EventArgs e) {
			if ((sender == null) || (e == null)) {
				return;
			}

			Logger.Warn($"Pin event initialized.");
		}

		public void OnLoaded() {
			Logger.Warn("Pin event loaded.");
		}

		public void OnStopped(object sender, EventArgs e) {
			if ((sender == null) || (e == null)) {
				return;
			}

			Logger.Warn("Events stopped.");
		}

		public void OnValueChanged(object sender, OnValueChangedEventArgs e) {
			if ((sender == null) || (e == null)) {
				return;
			}
			
			Logger.Warn($"[{nameof(OnValueChanged)}] | {e.TimeStamp} -> {e.Pin} -> {e.Current.State} ({e.Previous.State})");
		}
	}
}
