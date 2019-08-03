using System;
using System.Collections.Generic;
using System.Text;
using Assistant.PushBullet.ApiResponse;
using Assistant.PushBullet.Parameters;

namespace Assistant.PushBullet.Interfaces {
	public interface IPushBulletClient {
		string ClientAccessToken { get; set; }

		IUserDeviceListResponse GetCurrentDevices ();

		IPushResponse SendPush (IPushRequestContent content);

		IListSubscriptionsResponse GetSubscriptions ();

		PushEnums.PushDeleteStatusCode DeletePush (string pushIdentifier);

		IPushListResponse GetAllPushes (IPushListRequestContent listPushParams);

		IChannelInfoResponse GetChannelInfo (string channelTag);

	}
}
