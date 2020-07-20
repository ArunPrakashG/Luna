using System;
using System.Collections.Generic;
using System.Text;

namespace Luna.Pushbullet.Exceptions {
	public class InvalidRequestException : Exception {
		public InvalidRequestException() : base("Request cannot complete as the request contents are either incorrect or incomplete.") {
		}

		public InvalidRequestException(string message) : base(message) {
		}
	}
}
