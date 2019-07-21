using Assistant.PushBulletNotifications;
using System;
using System.Collections.Generic;
using System.Text;
using static Assistant.PushBulletNotifications.PushEnums;

namespace Assistant.PushBulletNotifications.Parameters {
	public class SendPushParams {
		public PushTarget PushTarget { get; set; }
		public PushType PushType { get; set; }
		public string PushTargetValue { get; set; }
		public string PushTitle { get; set; }
		public string PushBody { get; set; }
		public string LinkUrl { get; set; }
		public string FileName { get; set; }
		public string FileType { get; set; }
		public string FileUrl { get; set; }
	}
}
