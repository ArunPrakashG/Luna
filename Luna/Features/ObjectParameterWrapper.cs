using System;
using System.Collections.Generic;
using System.Text;

namespace Luna.Features {
	internal class ObjectParameterWrapper {
		internal readonly object[] Arguments;
		internal bool HasValue => Arguments != null && Arguments.Length > 0;

		internal ObjectParameterWrapper(params object[] args) {
			Arguments = args ?? new object[] { };
		}
	}
}
