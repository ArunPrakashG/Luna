using Luna.Gpio.PinEvents.PinEventArgs;
using Luna.TypeLoader;
using System;

namespace Luna.Gpio.PinEvents {
	internal interface IPinEvent : ILoadable {
		void OnValueChanged(object sender, OnValueChangedEventArgs e);

		void OnInitialized(object sender, EventArgs e);

		void OnStopped(object sender, EventArgs e);

		void OnActivated(object sender, OnActivatedEventArgs e);

		void OnDeactivated(object sender, OnDeactivatedEventArgs e);
	}
}
