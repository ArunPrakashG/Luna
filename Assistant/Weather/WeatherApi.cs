using HomeAssistant.Extensions;
using HomeAssistant.Log;
using Newtonsoft.Json;
using static HomeAssistant.AssistantCore.Enums;

namespace Assistant.Weather {
	public class WeatherApi {
		public ApiStructure.Rootobject ApiResponse { get; private set; }
		public WeatherData WeatherResult { get; set; }
		private Logger Logger { get; set; } = new Logger("WEATHER");

		public static string GenerateWeatherUrl(string apiKey, int pinCode = 689653, string countryCode = "in") => Helpers.IsNullOrEmpty(apiKey) ? null : Helpers.GetUrlToString($"https://api.openweathermap.org/data/2.5/weather?zip={pinCode},{countryCode}&appid={apiKey}", RestSharp.Method.GET);

		public (bool status, WeatherData response) GetWeatherInfo(string apiKey, int pinCode = 689653, string countryCode = "in") {
			if (Helpers.IsNullOrEmpty(apiKey)) {
				return (false, WeatherResult);
			}

			if (pinCode <= 0 || Helpers.IsNullOrEmpty(countryCode)) {
				return (false, WeatherResult);
			}

			(bool status, ApiStructure.Rootobject response) = FetchWeatherInfo(apiKey, pinCode, countryCode);

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
				WeatherResult.Rain3h = response.rain._3h;
				Logger.Log("successfully fetched weather info", LogLevels.Trace);
				return (true, WeatherResult);
			}
			else {
				Logger.Log("Could not fetch weather data", LogLevels.Trace);
				return (false, WeatherResult);
			}
		}

		public (bool status, ApiStructure.Rootobject response) FetchWeatherInfo(string apiKey, int pinCode = 689653, string countryCode = "in") {
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

			ApiResponse = JsonConvert.DeserializeObject<ApiStructure.Rootobject>(JSON);
			
			Logger.Log("Fetched weather information successfully", LogLevels.Trace);
			return (true, ApiResponse);
		}
	}
}
