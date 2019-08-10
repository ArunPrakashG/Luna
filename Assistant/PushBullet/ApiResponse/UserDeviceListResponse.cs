
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

using Newtonsoft.Json;

namespace Assistant.PushBullet.ApiResponse {
	public class UserDeviceListResponse {

		[JsonProperty("accounts")]
		public object[] Accounts { get; set; }
		[JsonProperty("blocks")]
		public object[] Blocks { get; set; }
		[JsonProperty("channels")]
		public object[] Channels { get; set; }
		[JsonProperty("chats")]
		public object[] Chats { get; set; }
		[JsonProperty("clients")]
		public object[] Clients { get; set; }
		[JsonProperty("contacts")]
		public object[] Contacts { get; set; }
		[JsonProperty("devices")]
		public Device[] Devices { get; set; }
		[JsonProperty("grants")]
		public object[] Grants { get; set; }
		[JsonProperty("pushes")]
		public object[] Pushes { get; set; }
		[JsonProperty("profiles")]
		public object[] Profiles { get; set; }
		[JsonProperty("subscriptions")]
		public object[] Subscriptions { get; set; }
		[JsonProperty("texts")]
		public object[] Texts { get; set; }

		public class Device {
			[JsonProperty("active")]
			public bool CurrentlyActive { get; set; }
			[JsonProperty("iden")]
			public string Identifier { get; set; }
			[JsonProperty("created")]
			public float Created { get; set; }
			[JsonProperty("modified")]
			public float LastModified { get; set; }
			[JsonProperty("type")]
			public string DeviceType { get; set; }
			[JsonProperty("kind")]
			public string DeviceKind { get; set; }
			[JsonProperty("nickname")]
			public string DeviceNickname { get; set; }
			[JsonProperty("manufacturer")]
			public string DeviceManufacturer { get; set; }
			[JsonProperty("model")]
			public string DeviceModel { get; set; }
			[JsonProperty("app_version")]
			public int AppVersion { get; set; }
			[JsonProperty("pushable")]
			public bool Pushable { get; set; }
			[JsonProperty("icon")]
			public string Icon { get; set; }
			[JsonProperty("fingerprint")]
			public string DeviceFingerprint { get; set; }
			[JsonProperty("push_token")]
			public string PushToken { get; set; }
			[JsonProperty("has_sms")]
			public bool HasSMSAbility { get; set; }
			[JsonProperty("has_mms")]
			public bool HasMMSAbility { get; set; }
			[JsonProperty("remote_files")]
			public string RemoteFiles { get; set; }
		}
	}
}
