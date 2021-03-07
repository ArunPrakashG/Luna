using exSharp;
using OpenWeatherApiSharp;
using Synergy.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Luna.Shell.InternalCommands {
	public class WeatherCommand : IShellCommand, IDisposable {
		public bool HasParameters => true;

		public string CommandName => "Weather Command";

		public bool IsInitSuccess { get; set; }

		public int MaxParameterCount => 3;

		public string CommandDescription => "Displays weather information about a particular locality.";

		public string CommandKey => "weather";

		public SemaphoreSlim Sync { get; set; }
		public Func<Parameter, bool> OnExecuteFunc { get; set; }

		public void Dispose() {
			IsInitSuccess = false;
			Sync.Dispose();
		}

		public async Task ExecuteAsync(Parameter parameter) {
			if (!IsInitSuccess) {
				return;
			}

			if (parameter.Parameters.Length > MaxParameterCount) {
				ShellIO.Error("Too many arguments.");
				return;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				if (OnExecuteFunc != null) {
					if (OnExecuteFunc.Invoke(parameter)) {
						return;
					}
				}

				string? apiKey;
				if (string.IsNullOrEmpty(Program.CoreInstance.GetCoreConfig().ApiKeys.OpenWeatherApiKey)) {
					ShellIO.Error("Weather API key isn't set.");

					apiKey = ShellIO.GetString("Open Weather Api Key");

					if (string.IsNullOrEmpty(apiKey)) {
						ShellIO.Error("Api key is invalid or not set properly.");
						return;
					}
				}

				apiKey = Program.CoreInstance.GetCoreConfig().ApiKeys.OpenWeatherApiKey;
				int pinCode;
				OpenWeatherApiSharp.WeatherResponse? weather;

				switch (parameter.ParameterCount) {
					case 0:
						ShellIO.Error("Pin code is invalid or not set.");
						return;
					case 1 when !string.IsNullOrEmpty(parameter.Parameters[0]):
						if (!int.TryParse(parameter.Parameters[0], out pinCode)) {
							ShellIO.Error("Failed to parse pin code. Entered pin code is invalid.");
							return;
						}

						using (OpenWeatherMapClient client = new OpenWeatherMapClient(apiKey)) {
							weather = await client.GetWeatherAsync(pinCode, "in").ConfigureAwait(false);
						}

						if (weather == null || weather.Location == null || weather.Wind == null || weather.Data == null) {
							ShellIO.Error("Weather request failed.");
							return;
						}

						ShellIO.Info($"---------- Weather Data | {weather.LocationName} | {weather.Location.Latitude}:{weather.Location.Longitude} ----------");
						ShellIO.Info($"Wind Speed: {weather.Wind.Speed}");
						ShellIO.Info($"Humidity: {weather.Data.Humidity}");
						ShellIO.Info($"Pressure: {weather.Data.Pressure}");
						ShellIO.Info($"Sea Level: {weather.Data.SeaLevel}");
						ShellIO.Info($"Temperature: {KelvinToCelsius(weather.Data.Temperature)} C");
						return;
					case 2 when !string.IsNullOrEmpty(parameter.Parameters[0]) && !string.IsNullOrEmpty(parameter.Parameters[1]):
						if (!int.TryParse(parameter.Parameters[0], out pinCode)) {
							ShellIO.Error("Pin code is invalid.");
							return;
						}

						if (parameter.Parameters[1].Length > 3) {
							ShellIO.Error("Country code is invalid.");
							return;
						}

						using (OpenWeatherMapClient client = new OpenWeatherMapClient(apiKey)) {
							weather = await client.GetWeatherAsync(pinCode, "in").ConfigureAwait(false);
						}

						if (weather == null || weather.Location == null || weather.Wind == null || weather.Data == null) {
							ShellIO.Error("Weather request failed.");
							return;
						}

						ShellIO.Info($"---------- Weather Data | {weather.LocationName} | {weather.Location.Latitude}:{weather.Location.Longitude} ----------");
						ShellIO.Info($"Wind Speed: {weather.Wind.Speed}");
						ShellIO.Info($"Humidity: {weather.Data.Humidity}");
						ShellIO.Info($"Pressure: {weather.Data.Pressure}");
						ShellIO.Info($"Sea Level: {weather.Data.SeaLevel}");
						ShellIO.Info($"Temperature: {KelvinToCelsius(weather.Data.Temperature)} C");
						return;
					case 3 when !string.IsNullOrEmpty(parameter.Parameters[0]) && !string.IsNullOrEmpty(parameter.Parameters[1]) && !string.IsNullOrEmpty(parameter.Parameters[2]):
						if (!int.TryParse(parameter.Parameters[0], out pinCode)) {
							ShellIO.Error("Pin code is invalid.");
							return;
						}

						if (parameter.Parameters[1].Length > 3) {
							ShellIO.Error("Country code is invalid.");
							return;
						}

						bool tts = parameter.Parameters[2].AsBool();

						using (OpenWeatherMapClient client = new OpenWeatherMapClient(apiKey)) {
							weather = await client.GetWeatherAsync(pinCode, "in").ConfigureAwait(false);
						}

						if (weather == null || weather.Location == null || weather.Wind == null || weather.Data == null) {
							ShellIO.Error("Weather request failed.");
							return;
						}

						if (tts) {
							Helpers.InBackground(() => {
								using (TTS tts = new TTS(false, false)) {
									tts.Speak($"Weather Data for {weather.LocationName}. " +
											$"Wind Speed is {weather.Wind.Speed}. " +
											$"Humidity level is {weather.Data.Humidity}. Pressure level {weather.Data.Pressure}. " +
											$"Sea Level is {weather.Data.SeaLevel}. Temperature is {weather.Data.Temperature}."
									);
								}
							});
						}

						ShellIO.Info($"---------- Weather Data | {weather.LocationName} | {weather.Location.Latitude}:{weather.Location.Longitude} ----------");
						ShellIO.Info($"Wind Speed: {weather.Wind.Speed}");
						ShellIO.Info($"Humidity: {weather.Data.Humidity}");
						ShellIO.Info($"Pressure: {weather.Data.Pressure}");
						ShellIO.Info($"Sea Level: {weather.Data.SeaLevel}");
						ShellIO.Info($"Temperature: {KelvinToCelsius(weather.Data.Temperature)} C");
						return;
					default:
						ShellIO.Error("Command seems to be in incorrect syntax.");
						return;
				}
			}
			catch (Exception e) {
				ShellIO.Exception(e);
				return;
			}
			finally {
				Sync.Release();
			}
		}

		private double KelvinToCelsius(double temp) {
			return Math.Round(temp - 273.15, 3);
		}

		public async Task InitAsync() {
			Sync = new SemaphoreSlim(1, 1);
			IsInitSuccess = true;
		}

		public void OnHelpExec(bool quickHelp) {
			if (quickHelp) {
				ShellIO.Info($"{CommandName} - {CommandKey} | {CommandDescription} | {CommandKey} -[pin_code] -[country_code]");
				return;
			}

			ShellIO.Info($"----------------- { CommandName} | {CommandKey} -----------------");
			ShellIO.Info($"|> {CommandDescription}");
			ShellIO.Info($"Basic Syntax -> ' {CommandKey} -[pin_code] '");
			ShellIO.Info($"Advanced -> ' {CommandKey} -[pin_code] -[country_code] '");
			ShellIO.Info($"Advanced with TTS -> ' {CommandKey} -[pin_code] -[country_code] -[tts (true/false)] '");
			ShellIO.Info($"----------------- ----------------------------- -----------------");
		}

		public bool Parse(Parameter parameter) {
			if (!IsInitSuccess) {
				return false;
			}

			return false;
		}
	}
}
