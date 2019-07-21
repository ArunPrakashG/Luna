using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.PushBulletNotifications.Exceptions {
	public class ResponseIsNullException : Exception {
		public ResponseIsNullException() : base("The api json response is null or empty.") {
		}

		public ResponseIsNullException(string message) : base(message) {
		}
	}
}
