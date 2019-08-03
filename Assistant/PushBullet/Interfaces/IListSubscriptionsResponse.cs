namespace Assistant.PushBullet.Interfaces {

	public interface IListSubscriptionsResponse {

		object[] Accounts { get; set; }

		object[] Blocks { get; set; }

		object[] Channels { get; set; }

		object[] Chats { get; set; }

		object[] Clients { get; set; }

		object[] Contacts { get; set; }

		object[] Devices { get; set; }

		object[] Grants { get; set; }

		object[] Pushes { get; set; }

		object[] Profiles { get; set; }

		ISubscription[] Subscriptions { get; set; }

		object[] Texts { get; set; }

	}

}
