using Assistant.PushBullet.Interfaces;

namespace Assistant.PushBullet.Interfaces {

	public interface IChannelInfoResponse {

		string Identifier { get; set; }

		string Name { get; set; }

		string Tag { get; set; }

		int SubscriberCount { get; set; }

		IRecent_Pushes[] RecentPushes { get; set; }

	}

}
