
//    _  _  ___  __  __ ___     _   ___ ___ ___ ___ _____ _   _  _ _____
//   | || |/ _ \|  \/  | __|   /_\ / __/ __|_ _/ __|_   _/_\ | \| |_   _|
//   | __ | (_) | |\/| | _|   / _ \\__ \__ \| |\__ \ | |/ _ \| .` | | |
//   |_||_|\___/|_|  |_|___| /_/ \_\___/___/___|___/ |_/_/ \_\_|\_| |_|
//

//MIT License

//Copyright(c) 2019 Arun Prakash
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;
using Assistant.PushBullet.ApiResponse;
using Assistant.PushBullet.Exceptions;
using Assistant.PushBullet.Parameters;
using static Assistant.AssistantCore.Enums;

namespace Assistant.PushBullet {
	public class PushBulletService {
		private Logger Logger { get; set; } = new Logger("PUSH-BULLET-SERVICE");
		public PushBulletClient BulletClient { get; private set; }
		public UserDeviceListResponse CachedPushDevices { get; private set; }
		public string AccessToken { get; private set; }
		public bool IsBroadcastServiceOnline { get; private set; }
		private PushRequestContent PreviousBroadcastMessage { get; set; } = new PushRequestContent();

		public PushBulletService(string accessToken) {
			if (!Helpers.IsNullOrEmpty(accessToken)) {
				AccessToken = accessToken;
			}
			else {
				throw new IncorrectAccessTokenException(Core.Config.PushBulletApiKey);
			}
		}

		public PushBulletService() {
			if (!Helpers.IsNullOrEmpty(Core.Config.PushBulletApiKey)) {
				AccessToken = Core.Config.PushBulletApiKey;
			}
			else {
				throw new IncorrectAccessTokenException(Core.Config.PushBulletApiKey);
			}
		}

		public (bool status, UserDeviceListResponse currentPushDevices) InitPushService() {
			if (!Core.IsNetworkAvailable) {
				Logger.Log("No internet connection available. cannot connect to PushBullet API.", LogLevels.Error);
				return (false, CachedPushDevices);
			}

			BulletClient = new PushBulletClient(AccessToken);

			try {
				CachedPushDevices = (UserDeviceListResponse) BulletClient.GetCurrentDevices();
			}
			catch (RequestFailedException) {
				Logger.Log("Request to the api failed", LogLevels.Warn);
				return (false, CachedPushDevices);
			}
			catch (IncorrectAccessTokenException) {
				Logger.Log("The specified access token is invalid", LogLevels.Warn);
				return (false, CachedPushDevices);
			}
			catch (NullReferenceException) {
				return (false, CachedPushDevices);
			}

			if (CachedPushDevices == null) {
				Logger.Log("Failed to load PushBullet serivce.", LogLevels.Warn);
				return (false, CachedPushDevices);
			}

			Logger.Log("Loaded PushBullet service.");
			IsBroadcastServiceOnline = true;
			return (true, CachedPushDevices);
		}

		public (bool broadcastStatus, PushResponse response) BroadcastMessage(PushRequestContent broadcastValue) {
			if (!Core.IsNetworkAvailable) {
				Logger.Log("No internet connection available. cannot connect to PushBullet API.", LogLevels.Error);
				return (false, null);
			}

			if (!IsBroadcastServiceOnline) {
				return (false, null);
			}

			if (PreviousBroadcastMessage.Equals(broadcastValue)) {
				return (false, null);
			}

			if (broadcastValue == null) {
				Logger.Log("Cannot broadcast as the required values are empty.", LogLevels.Warn);
				return (false, null);
			}

			PreviousBroadcastMessage = broadcastValue;
			PushResponse pushResponse;
			try {
				pushResponse = BulletClient.SendPush(broadcastValue);
			}
			catch (ParameterValueIsNullException) {
				Logger.Log("Parameter value is null.", LogLevels.Warn);
				return (false, null);
			}
			catch (ResponseIsNullException) {
				Logger.Log("The api response is null", LogLevels.Warn);
				return (false, null);
			}
			catch (RequestFailedException) {
				Logger.Log("Request to the api failed", LogLevels.Warn);
				return (false, null);
			}
			catch (NullReferenceException) {
				return (false, null);
			}

			return (true, pushResponse);
		}
	}
}
