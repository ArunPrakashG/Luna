
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

namespace Assistant.Weather {

	public class WeatherData {

		[JsonProperty]
		public float Logitude { get; set; }

		[JsonProperty]
		public float Latitude { get; set; }

		[JsonProperty]
		public string WeatherMain { get; set; }

		[JsonProperty]
		public string WeatherDescription { get; set; }

		[JsonProperty]
		public string WeatherIcon { get; set; }

		[JsonProperty]
		public float Temperature { get; set; }

		[JsonProperty]
		public float Pressure { get; set; }

		[JsonProperty]
		public float Humidity { get; set; }

		[JsonProperty]
		public float SeaLevel { get; set; }

		[JsonProperty]
		public float GroundLevel { get; set; }

		[JsonProperty]
		public float WindSpeed { get; set; }

		[JsonProperty]
		public float WindDegree { get; set; }

		[JsonProperty]
		public float Clouds { get; set; }

		[JsonProperty]
		public long TimeZone { get; set; }

		[JsonProperty]
		public string LocationName { get; set; }
	}
}
