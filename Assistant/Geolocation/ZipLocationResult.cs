using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Geolocation {
	public class ZipLocationResult {
		[JsonProperty]
		public string Message { get; set; }
		[JsonProperty]
		public string Status { get; set; }

		public List<PostOffice> PostOfficeCollection { get; set; } = new List<PostOffice>();		

		public class PostOffice {
			[JsonProperty]
			public string Name { get; set; }
			[JsonProperty]
			public string Description { get; set; }
			[JsonProperty]
			public string BranchType { get; set; }
			[JsonProperty]
			public string DeliveryStatus { get; set; }
			[JsonProperty]
			public string Taluk { get; set; }
			[JsonProperty]
			public string Circle { get; set; }
			[JsonProperty]
			public string District { get; set; }
			[JsonProperty]
			public string Division { get; set; }
			[JsonProperty]
			public string Region { get; set; }
			[JsonProperty]
			public string State { get; set; }
			[JsonProperty]
			public string Country { get; set; }

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
