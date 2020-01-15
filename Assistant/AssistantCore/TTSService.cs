using Assistant.Extensions;
using Assistant.Log;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.AssistantCore {
	public class SpeechServiceCache {
		public string? SpeechText { get; set; }
		public bool IsCompleted { get; set; }
		public string? SpeechFileName { get; set; }
	}

	public class TTS {
		private static readonly Logger Logger = new Logger("TTS");
		private static readonly SemaphoreSlim SpeechSemaphore = new SemaphoreSlim(1, 1);
		private static readonly SemaphoreSlim SpeechDownloadSemaphore = new SemaphoreSlim(1, 1);
		private static List<SpeechServiceCache> SpeechCache { get; set; } = new List<SpeechServiceCache>();

		public static async Task<bool> SpeakText(string text, bool enableAlert = false) {
			if (Core.Config.MuteAssistant || !Helpers.IsRaspberryEnvironment()) {
				return false;
			}

			if (!Core.IsNetworkAvailable) {
				return false;
			}

			if (Helpers.IsNullOrEmpty(text)) {
				Logger.Log("The text is empty or null!", Enums.LogLevels.Error);
				return false;
			}

			try {
				await SpeechSemaphore.WaitAsync().ConfigureAwait(false);
				SpeechServiceCache Cache = new SpeechServiceCache();

				if (!Directory.Exists(Constants.TextToSpeechDirectory)) {
					Directory.CreateDirectory(Constants.TextToSpeechDirectory);
				}

				if (File.Exists(Constants.TTSAlertFilePath) && enableAlert) {
					string executeResult = $"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.ResourcesDirectory} && play {Constants.TTSAlertFileName} -q".ExecuteBash(false);
					Logger.Log(executeResult, Enums.LogLevels.Trace);
					await Task.Delay(200).ConfigureAwait(false);
				}

				string fileName = string.Empty;

				if (SpeechCache.Count > 0) {
					SpeechServiceCache? cache = SpeechCache.Find(x => x.SpeechText != null && x.SpeechText.Equals(text, StringComparison.OrdinalIgnoreCase));

					if(cache != null && cache.SpeechFileName != null) {
						fileName = cache.SpeechFileName;
						Logger.Log("Using cached speech as a speech file with the specified text already exists!", Enums.LogLevels.Trace);
						goto PlaySound;
					}
					else {
						fileName = GetSpeechFile(text);
					}
				}
				else {					
					fileName = GetSpeechFile(text);
				}

				if (Helpers.IsNullOrEmpty(fileName)) {
					return false;
				}

				Cache.SpeechFileName = fileName;
				Cache.SpeechText = text;
				Cache.IsCompleted = true;
				SpeechCache.Add(Cache);

			PlaySound:
				string playingResult = $"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.TextToSpeechDirectory} && play {fileName} -q".ExecuteBash(false);
				Logger.Log(playingResult, Enums.LogLevels.Trace);
				await Task.Delay(500).ConfigureAwait(false);
				return true;
			}
			finally {
				SpeechSemaphore.Release();
			}
		}

		public static async Task AssistantVoice(Enums.SpeechContext context) {
			if (Core.Config.MuteAssistant || !Helpers.IsRaspberryEnvironment()) {
				return;
			}

			string playingResult;
			switch (context) {
				case Enums.SpeechContext.AssistantStartup:
					if (!File.Exists(Constants.StartupSpeechFilePath) && Core.CoreInitiationCompleted) {
						string textToSpeak = $"Hello sir! Your assistant is up and running!";
						await SpeakText(textToSpeak).ConfigureAwait(false);
						break;
					}

					playingResult = $"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.TextToSpeechDirectory} && play {Constants.StartupFileName} -q".ExecuteBash(false);
					Logger.Log(playingResult, Enums.LogLevels.Trace);
					break;
				case Enums.SpeechContext.AssistantShutdown:
					if (!File.Exists(Constants.ShutdownSpeechFilePath) && Core.CoreInitiationCompleted) {
						string textToSpeak = $"Sir, your assistant shutting down! Have a nice day!";
						await SpeakText(textToSpeak).ConfigureAwait(false);
						break;
					}

					playingResult = $"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.TextToSpeechDirectory} && play {Constants.ShutdownFileName} -q".ExecuteBash(false);
					Logger.Log(playingResult, Enums.LogLevels.Trace);
					break;
				case Enums.SpeechContext.NewEmaiNotification:
					if (!File.Exists(Constants.NewMailSpeechFilePath) && Core.CoreInitiationCompleted) {
						string textToSpeak = $"Sir, you recevied a new email!";
						await SpeakText(textToSpeak).ConfigureAwait(false);
						break;
					}

					playingResult = $"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.TextToSpeechDirectory} && play {Constants.NewMailFileName} -q".ExecuteBash(false);
					Logger.Log(playingResult, Enums.LogLevels.Trace);
					break;
				default:
					break;
			}
		}

		private static void SpeakText(string text, Enums.SpeechContext context, bool disableTTSalert = true) {
			if (Core.Config.MuteAssistant) {
				return;
			}

			if (Core.IsUnknownOs) {
				Logger.Log("TTS service disabled as we are running on unknown OS.", Enums.LogLevels.Warn);
				return;
			}

			if (!Core.IsNetworkAvailable) {
				Logger.Log("Network is unavailable. TTS won't run.", Enums.LogLevels.Warn);
				return;
			}

			if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text)) {
				Logger.Log("Text is null! line 33, TTSService.cs", Enums.LogLevels.Error);
				return;
			}

			if (!Directory.Exists(Constants.TextToSpeechDirectory)) {
				Directory.CreateDirectory(Constants.TextToSpeechDirectory);
			}

			if (File.Exists(Constants.TTSAlertFilePath) && !disableTTSalert) {
				Helpers.ExecuteCommand($"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.ResourcesDirectory} && play {Constants.TTSAlertFileName} -q", false);
			}

			byte[]? result;
			switch (context) {
				case Enums.SpeechContext.AssistantStartup:
					if (!File.Exists(Constants.StartupSpeechFilePath)) {
						Logger.Log($"{Core.AssistantName} startup tts sound doesn't exist, downloading the sound...", Enums.LogLevels.Trace);

						result = Helpers.GetUrlToBytes($"http://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&q={text}&tl=En-us", Method.GET, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36");
						Logger.Log("Fetched voice file bytes.", Enums.LogLevels.Trace);

						if (result == null || result.Length <= 0 ) {
							Logger.Log("result returned as null!", Enums.LogLevels.Error);
							return;
						}

						Logger.Log($"Writting to file => {Constants.StartupSpeechFilePath}", Enums.LogLevels.Trace);
						Helpers.WriteBytesToFile(result, Constants.StartupSpeechFilePath);
					}

					if (File.Exists(Constants.StartupSpeechFilePath)) {
						Task.Delay(500).Wait();
						Helpers.ExecuteCommand($"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.TextToSpeechDirectory} && play {Constants.StartupFileName} -q", false);
					}
					else {
						Logger.Log("An error occured, either download failed, or the file doesn't exist!", Enums.LogLevels.Error);
						return;
					}
					break;

				case Enums.SpeechContext.NewEmaiNotification:
					if (!File.Exists(Constants.NewMailSpeechFilePath)) {
						Logger.Log($"{Core.AssistantName} startup tts sound doesn't exist, downloading the sound...", Enums.LogLevels.Trace);

						result = Helpers.GetUrlToBytes($"http://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&q={text}&tl=En-us", Method.GET, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36");
						Logger.Log("Fetched voice file bytes.", Enums.LogLevels.Trace);

						if (result == null || result.Length <= 0) {
							Logger.Log("result returned as null!", Enums.LogLevels.Error);
							return;
						}

						Logger.Log($"Writting to file => {Constants.NewMailSpeechFilePath}", Enums.LogLevels.Trace);
						Helpers.WriteBytesToFile(result, Constants.NewMailSpeechFilePath);
					}

					if (File.Exists(Constants.NewMailSpeechFilePath)) {
						Helpers.ExecuteCommand($"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.TextToSpeechDirectory} && play {Constants.NewMailFileName} -q", false);
					}
					else {
						Logger.Log("An error occured, either download failed, or the file doesn't exist!", Enums.LogLevels.Error);
						return;
					}
					break;

				case Enums.SpeechContext.Custom:
					result = Helpers.GetUrlToBytes($"http://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&q={text}&tl=En-us", Method.GET, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36");
					Logger.Log("Fetched voice file bytes.", Enums.LogLevels.Trace);

					if (result == null || result.Length <= 0) {
						Logger.Log("result returned as null!", Enums.LogLevels.Error);
						return;
					}

					string fileName = $"{DateTime.Now.Ticks}.mp3";

					Logger.Log($"Writting to file => {fileName}", Enums.LogLevels.Trace);
					Helpers.WriteBytesToFile(result, Constants.TextToSpeechDirectory + "/" + fileName);
					Task.Delay(200).Wait();
					if (File.Exists(Constants.TextToSpeechDirectory + "/" + fileName)) {
						Helpers.ExecuteCommand($"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.TextToSpeechDirectory} && play {fileName} -q", false);
						Logger.Log($"Played the file {fileName} sucessfully", Enums.LogLevels.Trace);
					}
					else {
						Logger.Log("An error occured, either download failed, or the file doesn't exist!", Enums.LogLevels.Error);
						return;
					}
					break;
			}
		}

		private static string GetSpeechFile(string text) {
			if (Helpers.IsNullOrEmpty(text)) {
				return string.Empty;
			}

			if (!Core.IsNetworkAvailable) {
				return string.Empty;
			}

			try {
				SpeechDownloadSemaphore.Wait();
				string requestUrl = "https://translate.google.com/translate_tts?ie=UTF-8&total=1&idx=0&textlen=32&client=tw-ob&q=" + text + "&tl=en-us";
				RestClient client = new RestClient(requestUrl);
				RestRequest request = new RestRequest(Method.GET);
				client.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/76.0.3809.132 Safari/537.36";
				byte[] result;
				IRestResponse response = client.Execute(request);

				if (response.StatusCode != HttpStatusCode.OK) {
					Logger.Log("Failed to download. Status Code: " + response.StatusCode + "/" + response.ResponseStatus);
					return string.Empty;
				}

				result = response.RawBytes;

				if (result.Length <= 0 || result == null) {
					Logger.Log("result returned as null!", Enums.LogLevels.Error);
					return string.Empty;
				}

				string fileName = $"{DateTime.Now.Ticks}.mp3";

				Helpers.WriteBytesToFile(result, Constants.TextToSpeechDirectory + "/" + fileName);
				if (!File.Exists(Constants.TextToSpeechDirectory + "/" + fileName)) {
					Logger.Log("An error occured.", Enums.LogLevels.Warn);
					return string.Empty;
				}

				return fileName;
			}
			finally {
				SpeechDownloadSemaphore.Release();
			}
		}
	}
}
