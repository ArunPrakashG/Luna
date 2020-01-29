using Newtonsoft.Json;
using System.Collections.Generic;

namespace Assistant.Geolocation {
	public class ZipLocationResult {
		[JsonProperty]
		public string Message { get; set; } = string.Empty;
		[JsonProperty]
		public string Status { get; set; } = string.Empty;
		[JsonProperty]
		public List<PostOffice> PostOfficeCollection { get; set; } = new List<PostOffice>();

		public class PostOffice {
			[JsonProperty]
			public string Name { get; set; } = string.Empty;
			[JsonProperty]
			public string Description { get; set; } = string.Empty;
			[JsonProperty]
			public string BranchType { get; set; } = string.Empty;
			[JsonProperty]
			public string DeliveryStatus { get; set; } = string.Empty;
			[JsonProperty]
			public string Taluk { get; set; } = string.Empty;
			[JsonProperty]
			public string Circle { get; set; } = string.Empty;
			[JsonProperty]
			public string District { get; set; } = string.Empty;
			[JsonProperty]
			public string Division { get; set; } = string.Empty;
			[JsonProperty]
			public string Region { get; set; } = string.Empty;
			[JsonProperty]
			public string State { get; set; } = string.Empty;
			[JsonProperty]
			public string Country { get; set; } = string.Empty;

			public static explicit operator PostOffice(ZipLocationResponse.Postoffice v) {
				return new PostOffice() {
					Name = v.Name,
					Description = v.Description,
					BranchType = v.BranchType,
					DeliveryStatus = v.DeliveryStatus,
					Taluk = v.Taluk,
					Circle = v.Circle,
					District = v.District,
					Division = v.Division,
					Region = v.Region,
					State = v.State,
					Country = v.Country
				};
			}
		}
	}
}
