using Newtonsoft.Json;
using System;

namespace Luna.Pushbullet.Parameters.PushTypes {
	[Obsolete("Not Implemented yet.")]
	public class FileType {		
		[JsonProperty("type")]
		public readonly string TypeIdentifier = "file";

		[JsonProperty("body")]
		public readonly string Body;

		[JsonProperty("file_name")]
		public readonly string FileName;

		[JsonProperty("file_type")]
		public readonly string MimeType;

		[JsonProperty("file_url")]
		public readonly string FileUrl;

		public FileType(string _body, string _fileName, string _mimeType, string _fileUrl) {
			Body = _body;
			FileName = _fileName;
			MimeType = _mimeType;
			FileUrl = _fileUrl;
		}
	}
}
