using System;
using System.Collections.Generic;
using System.Text;
using static Luna.Gpio.Enums;

namespace Luna.Gpio.PinEvents.PinEventArgs {
	internal abstract class PinValueBase {
		internal readonly GpioPinState State;
		internal readonly bool DigitalValue;
		internal readonly GpioPinMode PinMode;
		internal readonly PinEventState EventState;

		internal PinValueBase(GpioPinState state, bool digitalValue, GpioPinMode pinMode, PinEventState eventState) {
			State = state;
			DigitalValue = digitalValue;
			PinMode = pinMode;
			EventState = eventState;
		}

		public override bool Equals(object? obj) {
			if(obj == null) {
				return false;
			}

			PinValueBase? v = obj as PinValueBase;

			if(v == null) {
				return false;
			}

			return v.State == this.State && v.PinMode == this.PinMode && v.EventState == this.EventState && v.DigitalValue == this.DigitalValue;
		}

		public override int GetHashCode() {
			return base.GetHashCode();
		}
	}
}
