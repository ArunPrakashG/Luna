
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
using Newtonsoft.Json.Serialization;

namespace Assistant.PushBullet.ApiResponse {
	public class PushListResponse {
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
		public object[] Cevices { get; set; }
		[JsonProperty("grants")]
		public object[] Grants { get; set; }
		[JsonProperty("pushes")]
		public Push[] Pushes { get; set; }
		[JsonProperty("profiles")]
		public object[] Profiles { get; set; }
		[JsonProperty("subscriptions")]
		public object[] Subscriptions { get; set; }
		[JsonProperty("texts")]
		public object[] Texts { get; set; }

		public class Push {
			[JsonProperty("accounts")]
			public bool IsActive { get; set; }
			[JsonProperty("iden")]
			public string Identifier { get; set; }
			[JsonProperty("created")]
			public float CreatedAt { get; set; }
			[JsonProperty("modified")]
			public float ModifedAt { get; set; }
			[JsonProperty("type")]
			public string Type { get; set; }
			[JsonProperty("dismissed")]
			public bool IsDismissed { get; set; }
			[JsonProperty("guid")]
			public string Guid { get; set; }
			[JsonProperty("direction")]
			public string Direction { get; set; }
			[JsonProperty("sender_iden")]
			public string SenderIdentifier { get; set; }
			[JsonProperty("sender_email")]
			public string SenderEmail { get; set; }
			[JsonProperty("sender_email_normalized")]
			public string SenderEmailNormalized { get; set; }
			[JsonProperty("sender_name")]
			public string SenderName { get; set; }
			[JsonProperty("receiver_iden")]
			public string ReceiverIdentifier { get; set; }
			[JsonProperty("receiver_email")]
			public string ReceiverEmail { get; set; }
			[JsonProperty("receiver_email_normalized")]
			public string ReceiverEmailNormalized { get; set; }
			[JsonProperty("source_device_iden")]
			public string SourceDeviceIdentifier { get; set; }
			[JsonProperty("awake_app_guids")]
			public string[] AwakeAppGuids { get; set; }
			[JsonProperty("body")]
			public string PushBody { get; set; }
			[JsonProperty("title")]
			public string PushTitle { get; set; }
		}

	}
}
