
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
	public class ChannelInfoResponse {
		[JsonProperty("iden")]
		public string Identifier { get; set; }
		[JsonProperty("name")]
		public string Name { get; set; }
		[JsonProperty("tag")]
		public string Tag { get; set; }
		[JsonProperty("subscriber_count")]
		public int SubscriberCount { get; set; }
		[JsonProperty("recent_pushes")]
		public Recent_Pushes[] RecentPushes { get; set; }

		public class Recent_Pushes {
			[JsonProperty("active")]
			public bool IsActive { get; set; }
			[JsonProperty("created")]
			public float CreatedAt { get; set; }
			[JsonProperty("modified")]
			public float ModifiedAt { get; set; }
			[JsonProperty("type")]
			public string Type { get; set; }
			[JsonProperty("dismissed")]
			public bool Dismissed { get; set; }
			[JsonProperty("guid")]
			public string Guid { get; set; }
			[JsonProperty("direction")]
			public string Direction { get; set; }
			[JsonProperty("sender_name")]
			public string SenderName { get; set; }
			[JsonProperty("channel_iden")]
			public string ChannelIdentifier { get; set; }
			[JsonProperty("body")]
			public string Body { get; set; }
		}
	}
}
