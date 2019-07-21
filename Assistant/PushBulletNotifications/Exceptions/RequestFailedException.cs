using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.PushBulletNotifications.Exceptions {
	public class RequestFailedException : Exception {
		public RequestFailedException() : base("A request to PushBullet api has been failed.") {
		}

		public RequestFailedException(string message) : base(message) {
		}
	}
}
