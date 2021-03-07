using Luna.Features;
using System;
using System.Collections.Generic;
using System.Text;

namespace Luna.External.Updates {
	internal class UpdateJob : InternalJob {
		public UpdateJob(TimeSpan scheduledSpan, ObjectParameterWrapper? parameters = null) : base(nameof(UpdateJob), scheduledSpan, parameters) {
		}

		protected override void OnDisposed() {
			throw new NotImplementedException();
		}

		protected override void OnJobInitialized() {
			throw new NotImplementedException();
		}

		protected override void OnJobLoaded() {
			throw new NotImplementedException();
		}

		protected override void OnJobTriggered(ObjectParameterWrapper? objectParameterWrapper) {
			throw new NotImplementedException();
		}
	}
}
