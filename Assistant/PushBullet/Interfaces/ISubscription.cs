namespace Assistant.PushBullet.Interfaces {

	public interface ISubscription {

		bool IsActive { get; set; }

		string Identifier { get; set; }

		float CreatedAt { get; set; }

		float ModifiedAt { get; set; }

		IChannel Channel { get; set; }

	}

}
