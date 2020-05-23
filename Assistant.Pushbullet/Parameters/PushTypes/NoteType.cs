using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Pushbullet.Parameters.PushTypes {
	public class NoteType {
		[JsonProperty("type")]
		public readonly string TypeIdentifier = "note";

		[JsonProperty("title")]
		public readonly string Title;

		[JsonProperty("body")]
		public readonly string Body;

		public NoteType(string _title, string _body) {
			Title = _title;
			Body = _body;
		}
	}
}
