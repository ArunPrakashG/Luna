namespace Assistant.PushBullet.Interfaces {

	public interface IPushListRequestContent {

		string ModifiedAfter { get; set; }

		bool ActiveOnly { get; set; }

		string Cursor { get; set; }

		int MaxResults { get; set; }

	}

}
