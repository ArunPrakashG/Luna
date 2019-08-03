using Assistant.PushBullet.Interfaces;
using static Assistant.PushBullet.PushEnums;

namespace Assistant.PushBullet.Parameters {
	public class PushRequestContent : IPushRequestContent {
		public PushTarget PushTarget { get; set; }
		public PushType PushType { get; set; }
		public string PushTargetValue { get; set; }
		public string PushTitle { get; set; }
		public string PushBody { get; set; }
		public string LinkUrl { get; set; }
		public string FileName { get; set; }
		public string FileType { get; set; }
		public string FileUrl { get; set; }

		public override int GetHashCode () => base.GetHashCode();

		public override bool Equals(object obj) {
			if (obj == null) {
				return false;
			}

			if (GetType() != obj.GetType()) {
				return false;
			}

			PushRequestContent Object = (PushRequestContent) obj;

			return (PushTarget == Object.PushTarget) && (PushType == Object.PushType) &&
				   (PushTargetValue == Object.PushTargetValue) && (PushTitle == Object.PushTitle) &&
				   (PushBody == Object.PushBody) && (LinkUrl == Object.LinkUrl) && (FileName == Object.FileName) &&
				   (FileType == Object.FileType) && (FileUrl == Object.FileUrl);
		}
	}
}
