using System;
using System.Collections.Generic;
using System.Text;

namespace Luna.Features {
	internal class JobEvents {
		internal Action<ObjectParameterWrapper> EventAction { get; set; }

		internal ObjectParameterWrapper EventStateArguments { get; set; }

		internal Action OnJobInitialized { get; set; }

		internal Action OnLoaded { get; set; }
	}
}
