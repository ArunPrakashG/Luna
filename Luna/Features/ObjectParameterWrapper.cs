using System;
using System.Collections.Generic;
using System.Text;

namespace Luna.Features {
	public class ObjectParameterWrapper {
		public readonly object[] Arguments;
		public bool HasValue => Arguments != null && Arguments.Length > 0;

		public ObjectParameterWrapper(params object[] args) {
			Arguments = args ?? new object[] { };
		}
	}
}
