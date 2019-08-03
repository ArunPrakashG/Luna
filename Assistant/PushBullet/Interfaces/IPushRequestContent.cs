namespace Assistant.PushBullet.Interfaces {

	public interface IPushRequestContent {

		PushEnums.PushTarget PushTarget { get; set; }

		PushEnums.PushType PushType { get; set; }

		string PushTargetValue { get; set; }

		string PushTitle { get; set; }

		string PushBody { get; set; }

		string LinkUrl { get; set; }

		string FileName { get; set; }

		string FileType { get; set; }

		string FileUrl { get; set; }

	}

}
