
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
using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Geolocation {
	public class ZipLocationResult {
		[JsonProperty]
		public string Message { get; set; }
		[JsonProperty]
		public string Status { get; set; }
		[JsonProperty]
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
