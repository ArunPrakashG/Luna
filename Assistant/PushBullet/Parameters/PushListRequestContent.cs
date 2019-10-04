namespace Assistant.PushBullet.Parameters {
	public class PushListRequestContent {
		public string ModifiedAfter { get; set; } = string.Empty;
		public bool ActiveOnly { get; set; } = false;
		public string Cursor { get; set; } = string.Empty;
		public int MaxResults { get; set; } = 0;
	}
}
