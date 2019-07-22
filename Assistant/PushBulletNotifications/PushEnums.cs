namespace Assistant.PushBulletNotifications {
	public class PushEnums {
		public enum PushType {
			Note,
			Link,
			File
		}

		public enum PushTarget {
			Device,
			Email,
			Channel,
			Client,
			All
		}

		public enum PushDeleteStatusCode {
			ObjectNotFound,
			Success,
			Unknown
		}
	}
}
