using Assistant.Extensions;
using Assistant.Extensions.Shared.Shell;
using Assistant.Sound.Speech;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Core.Shell.InternalCommands {
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
				ShellOut.Error("Too many arguments.");
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
				if (string.IsNullOrEmpty(Core.Config.OpenWeatherApiKey)) {
					ShellOut.Error("Weather API key isn't set.");

					apiKey = ShellOut.ShellIn_String("Open Weather Api Key");

					if (string.IsNullOrEmpty(apiKey)) {
						ShellOut.Error("Api key is invalid or not set properly.");
						return;
					}
				}

				apiKey = Core.Config.OpenWeatherApiKey;
				int pinCode;
				Weather.WeatherResponse? weather;

				switch (parameter.ParameterCount) {
					case 0:
						ShellOut.Error("Pin code is invalid or not set.");
						return;
					case 1 when !string.IsNullOrEmpty(parameter.Parameters[0]):
						if (!int.TryParse(parameter.Parameters[0], out pinCode)) {
							ShellOut.Error("Failed to parse pin code. Entered pin code is invalid.");
							return;
						}

						weather = await Core.WeatherClient.GetWeather(apiKey, pinCode, "in").ConfigureAwait(false);

						if (weather == null || weather.Location == null || weather.Wind == null || weather.Data == null) {
							ShellOut.Error("Weather request failed.");
							return;
						}

						ShellOut.Info($"---------- Weather Data | {weather.LocationName} | {weather.Location.Latitude}:{weather.Location.Longitude} ----------");
						ShellOut.Info($"Wind Speed: {weather.Wind.Speed}");
						ShellOut.Info($"Humidity: {weather.Data.Humidity}");
						ShellOut.Info($"Pressure: {weather.Data.Pressure}");
						ShellOut.Info($"Sea Level: {weather.Data.SeaLevel}");
						ShellOut.Info($"Temperature: {weather.Data.Temperature}");
						return;
					case 2 when !string.IsNullOrEmpty(parameter.Parameters[0]) && !string.IsNullOrEmpty(parameter.Parameters[1]):
						if (!int.TryParse(parameter.Parameters[0], out pinCode)) {
							ShellOut.Error("Pin code is invalid.");
							return;
						}

						if (parameter.Parameters[1].Length > 3) {
							ShellOut.Error("Country code is invalid.");
							return;
						}

						weather = await Core.WeatherClient.GetWeather(apiKey, pinCode, parameter.Parameters[1]).ConfigureAwait(false);

						if (weather == null || weather.Location == null || weather.Wind == null || weather.Data == null) {
							ShellOut.Error("Weather request failed.");
							return;
						}

						ShellOut.Info($"---------- Weather Data | {weather.LocationName} | {weather.Location.Latitude}:{weather.Location.Longitude} ----------");
						ShellOut.Info($"Wind Speed: {weather.Wind.Speed}");
						ShellOut.Info($"Humidity: {weather.Data.Humidity}");
						ShellOut.Info($"Pressure: {weather.Data.Pressure}");
						ShellOut.Info($"Sea Level: {weather.Data.SeaLevel}");
						ShellOut.Info($"Temperature: {weather.Data.Temperature}");
						return;
					case 3 when !string.IsNullOrEmpty(parameter.Parameters[0]) && !string.IsNullOrEmpty(parameter.Parameters[1]) && !string.IsNullOrEmpty(parameter.Parameters[2]):
						if (!int.TryParse(parameter.Parameters[0], out pinCode)) {
							ShellOut.Error("Pin code is invalid.");
							return;
						}

						if (parameter.Parameters[1].Length > 3) {
							ShellOut.Error("Country code is invalid.");
							return;
						}

						if (!parameter.Parameters[2].AsBool(out bool? tts)) {
							ShellOut.Error("'TTS' argument is invalid.");
							return;
						}

						weather = await Core.WeatherClient.GetWeather(apiKey, pinCode, parameter.Parameters[1]).ConfigureAwait(false);

						if (weather == null || weather.Location == null || weather.Wind == null || weather.Data == null) {
							ShellOut.Error("Weather request failed.");
							return;
						}

						if (tts != null && tts.HasValue && tts.Value) {
							Helpers.InBackground(async () => await TTS.SpeakText($"Weather Data for {weather.LocationName}. " +
								$"Wind Speed is {weather.Wind.Speed}. " +
								$"Humidity level is {weather.Data.Humidity}. Pressure level {weather.Data.Pressure}. " +
								$"Sea Level is {weather.Data.SeaLevel}. Temperature is {weather.Data.Temperature}.", true));
						}

						ShellOut.Info($"---------- Weather Data | {weather.LocationName} | {weather.Location.Latitude}:{weather.Location.Longitude} ----------");
						ShellOut.Info($"Wind Speed: {weather.Wind.Speed}");
						ShellOut.Info($"Humidity: {weather.Data.Humidity}");
						ShellOut.Info($"Pressure: {weather.Data.Pressure}");
						ShellOut.Info($"Sea Level: {weather.Data.SeaLevel}");
						ShellOut.Info($"Temperature: {weather.Data.Temperature}");
						return;
					default:
						ShellOut.Error("Command seems to be in incorrect syntax.");
						return;
				}
			}
			catch (Exception e) {
				ShellOut.Exception(e);
				return;
			}
			finally {
				Sync.Release();
			}
		}

		public async Task InitAsync() {
			Sync = new SemaphoreSlim(1, 1);
			IsInitSuccess = true;
		}

		public void OnHelpExec(bool quickHelp) {
			if (quickHelp) {
				ShellOut.Info($"{CommandName} - {CommandKey} | {CommandDescription} | {CommandKey} -[pin_code], -[country_code];");
				return;
			}

			ShellOut.Info($"----------------- { CommandName} | {CommandKey} -----------------");
			ShellOut.Info($"|> {CommandDescription}");
			ShellOut.Info($"Basic Syntax -> ' {CommandKey} -[pin_code]; '");
			ShellOut.Info($"Advanced -> ' {CommandKey} -[pin_code], -[country_code]; '");
			ShellOut.Info($"Advanced with TTS -> ' {CommandKey} -[pin_code], -[country_code] -[tts (true/false)]; '");
			ShellOut.Info($"----------------- ----------------------------- -----------------");
		}

		public bool Parse(Parameter parameter) {
			if (!IsInitSuccess) {
				return false;
			}

			return false;
		}
	}
}
