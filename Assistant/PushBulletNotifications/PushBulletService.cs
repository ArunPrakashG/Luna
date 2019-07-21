using HomeAssistant.Log;
using PushbulletSharp;
using PushbulletSharp.Models.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.PushBulletNotifications {
	public class PushBulletService {
		private Logger Logger { get; set; } = new Logger("PUSH-BULLET");
		public PushbulletClient BulletClient { get; private set; }
		public UserDevices CachedPushDevices { get; private set; }

		public PushBulletService() {
		}

		//TODO: init service
		public (bool status, PushbulletClient bulletClient, UserDevices currentPushDevices) InitPushService() {
			
		}
	}
}
