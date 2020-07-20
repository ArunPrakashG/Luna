using static Luna.Pushbullet.PushEnums;

namespace Luna.Pushbullet.Parameters {
	public struct PushRequestParameter<T> {
		public readonly PushTarget PushTarget;
		public readonly string PushTargetValue;
		public readonly T PushType;		

		public PushRequestParameter(T _pushType, PushTarget _target, string _targetValue) {
			PushTarget = _target;
			PushTargetValue = _targetValue;
			PushType = _pushType;
		}
	}
}
