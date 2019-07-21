using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.PushBulletNotifications.Exceptions {
	class ParameterValueIsNullException : Exception {
		public ParameterValueIsNullException() : base("The parameter value is null.") {
		}

		public ParameterValueIsNullException(string message, string parameter) : base($"{message} ({parameter})") {
		}

		public ParameterValueIsNullException(string parameter) : base($"The parameter value is null. ({parameter})") {
		}
	}
}
