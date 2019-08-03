namespace Assistant.PushBullet.Interfaces {

	public interface IPush {

		bool IsActive { get; set; }

		string Identifier { get; set; }

		float CreatedAt { get; set; }

		float ModifedAt { get; set; }

		string Type { get; set; }

		bool IsDismissed { get; set; }

		string Guid { get; set; }

		string Direction { get; set; }

		string SenderIdentifier { get; set; }

		string SenderEmail { get; set; }

		string SenderEmailNormalized { get; set; }

		string SenderName { get; set; }

		string ReceiverIdentifier { get; set; }

		string ReceiverEmail { get; set; }

		string ReceiverEmailNormalized { get; set; }

		string SourceDeviceIdentifier { get; set; }

		string[] AwakeAppGuids { get; set; }

		string PushBody { get; set; }

		string PushTitle { get; set; }

	}

}
