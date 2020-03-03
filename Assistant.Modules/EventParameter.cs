using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Modules {
	public struct EventParameter {
		public readonly object[] Values;
		public readonly int ValuesCount => Values != null ? Values.Length : 0;

		public EventParameter(object[] vals) {
			Values = vals;
		}
	}
}
