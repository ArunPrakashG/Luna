using System;

namespace Assistant.Location {
	[Serializable]
	public class Response {
		public string? Message { get; set; }
		public string? Status { get; set; }
		public Postoffice[]? PostOffice { get; set; }

		[Serializable]
		public class Postoffice {
			public string? Name { get; set; }
			public string? Description { get; set; }
			public string? BranchType { get; set; }
			public string? DeliveryStatus { get; set; }
			public string? Taluk { get; set; }
			public string? Circle { get; set; }
			public string? District { get; set; }
			public string? Division { get; set; }
			public string? Region { get; set; }
			public string? State { get; set; }
			public string? Country { get; set; }
		}
	}
}
