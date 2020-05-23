using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Pushbullet.Parameters.PushTypes {
	public class LinkType {
		[JsonProperty("type")]
		public readonly string TypeIdentifier = "link";

		[JsonProperty("title")]
		public readonly string Title;

		[JsonProperty("body")]
		public readonly string Body;

		[JsonProperty("url")]
		public readonly string Url;

		public LinkType(string _title, string _body, string _url) {
			Title = _title;
			Body = _body;
			Url = _url;
		}
	}
}
