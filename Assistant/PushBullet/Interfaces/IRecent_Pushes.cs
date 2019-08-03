namespace Assistant.PushBullet.Interfaces {

	public interface IRecent_Pushes {

		bool IsActive { get; set; }

		float CreatedAt { get; set; }

		float ModifiedAt { get; set; }

		string Type { get; set; }

		bool Dismissed { get; set; }

		string Guid { get; set; }

		string Direction { get; set; }

		string SenderName { get; set; }

		string ChannelIdentifier { get; set; }

		string Body { get; set; }

	}

}
