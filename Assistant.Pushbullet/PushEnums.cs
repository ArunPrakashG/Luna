namespace Luna.Pushbullet {
	public class PushEnums {
		public enum PushTarget {
			Device,
			Email,
			Channel,
			Client,
			All
		}

		public enum EPUSH_ROUTES {
			GET_DEVICES,
			PUSH,
			GET_SUBSCRIPTIONS,
			DELETE_PUSH,
			GET_ALL_PUSHES,
			GET_CHANNEL_INFO
		}

		public enum PushDeleteStatusCode {
			ObjectNotFound,
			Success,
			Unknown
		}
	}
}
