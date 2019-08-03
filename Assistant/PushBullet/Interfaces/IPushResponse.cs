namespace Assistant.PushBullet.Interfaces {

	public interface IPushResponse {

		bool IsActive { get; set; }

		string PushIdentifier { get; set; }

		float CreatedAt { get; set; }

		float ModifiedAt { get; set; }

		string PushType { get; set; }

		bool IsDismissed { get; set; }

		string Direction { get; set; }

		string SenderIdentifier { get; set; }

		string SenderEmail { get; set; }

		string SenderEmailNormalized { get; set; }

		string SenderName { get; set; }

		string ReceiverIdentifier { get; set; }

		string ReceiverEmail { get; set; }

		string ReceiverEmailNormalized { get; set; }

		string PushTitle { get; set; }

		string PushBody { get; set; }

	}

}
