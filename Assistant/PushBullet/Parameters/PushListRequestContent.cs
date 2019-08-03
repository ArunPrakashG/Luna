using Assistant.PushBullet.Interfaces;

namespace Assistant.PushBullet.Parameters {
	public class PushListRequestContent : IPushListRequestContent {
		public string ModifiedAfter { get; set; }
		public bool ActiveOnly { get; set; } = false;
		public string Cursor { get; set; }
		public int MaxResults { get; set; } = 0;
	}
}
