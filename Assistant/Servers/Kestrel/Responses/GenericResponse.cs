using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using Assistant.AssistantCore;

namespace Assistant.Servers.Kestrel.Responses {

	public class GenericResponse<T> {

		[JsonProperty(Required = Required.Always)]
		[Required]
		public T Result { get; set; }

		[JsonProperty(Required = Required.Always)]
		[Required]
		public string Response { get; set; }

		[JsonProperty(Required = Required.Always)]
		[Required]
		public Enums.HttpStatusCodes ResponseCode { get; set; }

		[JsonProperty(Required = Required.Always)]
		[Required]
		public DateTime DateTime { get; set; }

		public GenericResponse([NotNull] T result, Enums.HttpStatusCodes responseCode, DateTime commandTime) {
			if (commandTime == null) {
				throw new ArgumentNullException("commandTime is null.");
			}

			Result = result;
			Response = responseCode.ToString();
			ResponseCode = responseCode;
			DateTime = commandTime;
		}

		public GenericResponse([NotNull] T result, [NotNull] string response, Enums.HttpStatusCodes responseCode, DateTime commandTime) {
			if (commandTime == null) {
				throw new ArgumentNullException("commandTime is null.");
			}

			Result = result;
			Response = response;
			ResponseCode = responseCode;
			DateTime = commandTime;
		}
	}
}
