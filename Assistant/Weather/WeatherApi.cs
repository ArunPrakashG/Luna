using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;
using Newtonsoft.Json;
using static Assistant.AssistantCore.Enums;

namespace Assistant.Weather {
	public class WeatherApi {
		public ApiResponseStructure.Rootobject ApiResponse { get; private set; } = new ApiResponseStructure.Rootobject();
		public WeatherData WeatherResult { get; set; } = new WeatherData();
		private Logger Logger { get; set; } = new Logger("WEATHER");

		public static string GenerateWeatherUrl(string apiKey, int pinCode = 689653, string countryCode = "in") => Helpers.IsNullOrEmpty(apiKey) ? null : Helpers.GetUrlToString($"https://api.openweathermap.org/data/2.5/weather?zip={pinCode},{countryCode}&appid={apiKey}", RestSharp.Method.GET);

		public (bool status, WeatherData response) GetWeatherInfo(string apiKey, int pinCode = 689653, string countryCode = "in") {
			if (!Core.IsNetworkAvailable) {
				Logger.Log("Cannot continue as network isn't available.", LogLevels.Warn);
				return (false, WeatherResult);
			}

			if (Helpers.IsNullOrEmpty(apiKey)) {
				return (false, WeatherResult);
			}

			if (pinCode <= 0 || Helpers.IsNullOrEmpty(countryCode)) {
				return (false, WeatherResult);
			}

			(bool status, ApiResponseStructure.Rootobject response) = FetchWeatherInfo(apiKey, pinCode, countryCode);

			if (status) {
				WeatherResult.Latitude = response.coord.lat;
				WeatherResult.Logitude = response.coord.lon;
				WeatherResult.Temperature = response.main.temp;
				WeatherResult.WeatherMain = response.weather[0].main;
				WeatherResult.WeatherIcon = response.weather[0].icon;
				WeatherResult.WeatherDescription = response.weather[0].description;
				WeatherResult.Pressure = response.main.pressure;
				WeatherResult.Humidity = response.main.humidity;
				WeatherResult.SeaLevel = response.main.sea_level;
				WeatherResult.GroundLevel = response.main.grnd_level;
				WeatherResult.WindSpeed = response.wind.speed;
				WeatherResult.WindDegree = response.wind.deg;
				WeatherResult.Clouds = response.clouds.all;
				WeatherResult.TimeZone = response.timezone;
				WeatherResult.LocationName = response.name;
				Logger.Log("Assigined weather info values", LogLevels.Trace);
				return (true, WeatherResult);
			}
			else {
				Logger.Log("failed to assign weather info values", LogLevels.Trace);
				return (false, WeatherResult);
			}
		}

		private (bool status, ApiResponseStructure.Rootobject response) FetchWeatherInfo(string apiKey, int pinCode = 689653, string countryCode = "in") {
			if (Helpers.IsNullOrEmpty(apiKey)) {
				return (false, ApiResponse);
			}

			if (pinCode <= 0 || Helpers.IsNullOrEmpty(countryCode)) {
				return (false, ApiResponse);
			}

			string JSON = GenerateWeatherUrl(apiKey, pinCode, countryCode);

			if (Helpers.IsNullOrEmpty(JSON)) {
				Logger.Log("Failed to fetch api response from api.openweathermap.org", LogLevels.Warn);
				return (false, ApiResponse);
			}

			ApiResponse = JsonConvert.DeserializeObject<ApiResponseStructure.Rootobject>(JSON);
			
			Logger.Log("Fetched weather information successfully", LogLevels.Trace);
			return (true, ApiResponse);
		}
	}
}
