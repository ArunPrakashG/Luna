using Assistant.PushBullet.Interfaces;

namespace Assistant.PushBullet.Interfaces {

	public interface IUserDeviceListResponse {

		object[] Accounts { get; set; }

		object[] Blocks { get; set; }

		object[] Channels { get; set; }

		object[] Chats { get; set; }

		object[] Clients { get; set; }

		object[] Contacts { get; set; }

		IDevice[] Devices { get; set; }

		object[] Grants { get; set; }

		object[] Pushes { get; set; }

		object[] Profiles { get; set; }

		object[] Subscriptions { get; set; }

		object[] Texts { get; set; }

	}

}
