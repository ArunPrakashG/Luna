namespace Assistant.Core.Geolocation {
	public class ZipLocationResponse {
		public string Message { get; set; } = string.Empty;
		public string Status { get; set; } = string.Empty;
		public Postoffice[]? PostOffice { get; set; }

		public class Postoffice {
			public string Name { get; set; } = string.Empty;
			public string Description { get; set; } = string.Empty;
			public string BranchType { get; set; } = string.Empty;
			public string DeliveryStatus { get; set; } = string.Empty;
			public string Taluk { get; set; } = string.Empty;
			public string Circle { get; set; } = string.Empty;
			public string District { get; set; } = string.Empty;
			public string Division { get; set; } = string.Empty;
			public string Region { get; set; } = string.Empty;
			public string State { get; set; } = string.Empty;
			public string Country { get; set; } = string.Empty;
		}
	}
}
