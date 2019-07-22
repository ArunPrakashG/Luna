using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.PushBulletNotifications.Parameters {
	public class ListPushParams {
		public string ModifiedAfter { get; set; }
		public bool ActiveOnly { get; set; } = false;
		public string Cursor { get; set; }
		public int MaxResults { get; set; } = 0;
	}
}
