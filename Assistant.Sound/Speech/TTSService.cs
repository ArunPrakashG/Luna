using Assistant.Extensions;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static Assistant.Logging.Enums;

namespace Assistant.Sound.Speech {
	public class TTS {
		private static readonly ILogger Logger = new Logger(typeof(TTS).Name);
		private static readonly SemaphoreSlim SpeechSemaphore = new SemaphoreSlim(1, 1);
		private static readonly SemaphoreSlim SpeechDownloadSemaphore = new SemaphoreSlim(1, 1);
		private static List<SpeechServiceCache> SpeechCache { get; set; } = new List<SpeechServiceCache>();

		public static async Task<bool> SpeakText(string text, bool enableAlert = false) {
			if (!Sound.IsSoundAllowed || !Helpers.IsNetworkAvailable()) {
				return false;
			}

			if (string.IsNullOrEmpty(text)) {
				Logger.Error("The text is empty or null!");
				return false;
			}

			await SpeechSemaphore.WaitAsync().ConfigureAwait(false);
			try {
				SpeechServiceCache Cache = new SpeechServiceCache();

				if (!Directory.Exists(Constants.TextToSpeechDirectory)) {
					Directory.CreateDirectory(Constants.TextToSpeechDirectory);
				}

				if (File.Exists(Constants.TTSAlertFilePath) && enableAlert) {
					string? executeResult = $"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.ResourcesDirectory} && play {Constants.TTSAlertFileName} -q".ExecuteBash(false);
					Logger.Log(executeResult, LogLevels.Trace);
					await Task.Delay(200).ConfigureAwait(false);
				}

				string? fileName;

				if (SpeechCache.Count > 0) {
					SpeechServiceCache? cache = SpeechCache.Find(x => x.SpeechText != null && x.SpeechText.Equals(text, StringComparison.OrdinalIgnoreCase));

					if (cache != null && cache.SpeechFileName != null) {
						fileName = cache.SpeechFileName;
						Logger.Log("Using cached speech as a speech file with the specified text already exists!", LogLevels.Trace);
						goto PlaySound;
					}
					else {
						fileName = await GetSpeechFile(text).ConfigureAwait(false);
					}
				}
				else {
					fileName = await GetSpeechFile(text).ConfigureAwait(false);
				}

				if (string.IsNullOrEmpty(fileName)) {
					return false;
				}

				Cache.SpeechFileName = fileName;
				Cache.SpeechText = text;
				Cache.IsCompleted = true;
				SpeechCache.Add(Cache);

			PlaySound:
				string? playingResult = $"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.TextToSpeechDirectory} && play {fileName} -q".ExecuteBash(false);
				Logger.Log(playingResult, LogLevels.Trace);
				await Task.Delay(500).ConfigureAwait(false);
				return true;
			}
			finally {
				SpeechSemaphore.Release();
			}
		}

		public static async Task AssistantVoice(ESPEECH_CONTEXT context) {
			if (!Sound.IsSoundAllowed) {
				return;
			}

			string? playingResult;
			switch (context) {
				case ESPEECH_CONTEXT.AssistantStartup:
					if (!File.Exists(Constants.StartupSpeechFilePath)) {
						string textToSpeak = $"Hello sir! Your assistant is up and running!";
						await SpeakText(textToSpeak).ConfigureAwait(false);
						break;
					}

					playingResult = $"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.TextToSpeechDirectory} && play {Constants.StartupFileName} -q".ExecuteBash(false);
					Logger.Log(playingResult, LogLevels.Trace);
					break;
				case ESPEECH_CONTEXT.AssistantShutdown:
					if (!File.Exists(Constants.ShutdownSpeechFilePath)) {
						string textToSpeak = $"Sir, your assistant shutting down! Have a nice day!";
						await SpeakText(textToSpeak).ConfigureAwait(false);
						break;
					}

					playingResult = $"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.TextToSpeechDirectory} && play {Constants.ShutdownFileName} -q".ExecuteBash(false);
					Logger.Log(playingResult, LogLevels.Trace);
					break;
				case ESPEECH_CONTEXT.NewEmaiNotification:
					if (!File.Exists(Constants.NewMailSpeechFilePath)) {
						string textToSpeak = $"Sir, you received a new email!";
						await SpeakText(textToSpeak).ConfigureAwait(false);
						break;
					}

					playingResult = $"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.TextToSpeechDirectory} && play {Constants.NewMailFileName} -q".ExecuteBash(false);
					Logger.Log(playingResult, LogLevels.Trace);
					break;
				default:
					break;
			}
		}

		private static async Task<string?> GetSpeechFile(string text) {
			if (string.IsNullOrEmpty(text)) {
				return null;
			}

			if (!Helpers.IsNetworkAvailable()) {
				return null;
			}

			try {
				await SpeechDownloadSemaphore.WaitAsync().ConfigureAwait(false);

				string requestUrl = "https://translate.google.com/translate_tts?ie=UTF-8&total=1&idx=0&textlen=32&client=tw-ob&q=" + text + "&tl=en-us";
				RestClient client = new RestClient(requestUrl);
				RestRequest request = new RestRequest(Method.GET);
				client.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/76.0.3809.132 Safari/537.36";
				byte[] result;
				IRestResponse response = client.Execute(request);

				if (response.StatusCode != HttpStatusCode.OK) {
					Logger.Log("Failed to download. Status Code: " + response.StatusCode + "/" + response.ResponseStatus);
					return null;
				}

				result = response.RawBytes;

				if (result.Length <= 0 || result == null) {
					Logger.Log("result returned as null!", LogLevels.Error);
					return string.Empty;
				}

				string fileName = $"{DateTime.Now.Ticks}.mp3";

				Helpers.WriteBytesToFile(result, Constants.TextToSpeechDirectory + "/" + fileName);
				if (!File.Exists(Constants.TextToSpeechDirectory + "/" + fileName)) {
					Logger.Log("An error occurred.", LogLevels.Warn);
					return null;
				}

				return fileName;
			}
			finally {
				SpeechDownloadSemaphore.Release();
			}
		}

		public enum ESPEECH_CONTEXT : byte {
			AssistantStartup,
			AssistantShutdown,
			NewEmaiNotification,
			Custom
		}
	}
}
