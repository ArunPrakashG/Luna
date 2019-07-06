using Google.Cloud.Speech.V1;
using HomeAssistant.Extensions;
using HomeAssistant.Log;
using RestSharp;
using System;
using System.IO;
using System.Threading.Tasks;
using static HomeAssistant.AssistantCore.Enums;
using SpeechContext = HomeAssistant.AssistantCore.Enums.SpeechContext;

namespace HomeAssistant.AssistantCore {
	public class TTSService {
		private static readonly Logger Logger = new Logger("GOOGLE-SPEECH");

		public TTSService() {
		}

		private void SpeechToTextFromFile(string filePath) {
			SpeechClient speech = SpeechClient.Create();

			RecognizeResponse response = speech.Recognize(new RecognitionConfig() {
				Encoding = RecognitionConfig.Types.AudioEncoding.Flac,
				SampleRateHertz = 16000,
				LanguageCode = LanguageCodes.Malayalam.India
			}, RecognitionAudio.FromFile(filePath));

			foreach (SpeechRecognitionResult result in response.Results) {
				foreach (SpeechRecognitionAlternative alternative in result.Alternatives) {
					Logger.Log(alternative.Transcript, LogLevels.Info);
				}
			}
		}

		public static void SpeakText(string text, SpeechContext context, bool disableTTSalert = true) {
			if (Core.Config.MuteAssistant) {
				return;
			}

			if (Core.IsUnknownOs) {
				Logger.Log("TTS service disabled as we are running on unknown OS.", LogLevels.Warn);
				return;
			}

			if (!Core.IsNetworkAvailable) {
				Logger.Log("Network is unavailable. TTS won't run.", LogLevels.Warn);
				return;
			}

			if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text)) {
				Logger.Log("Text is null! line 33, TTSService.cs", LogLevels.Error);
				return;
			}

			if (!Directory.Exists(Constants.TextToSpeechDirectory)) {
				Directory.CreateDirectory(Constants.TextToSpeechDirectory);
			}

			if (File.Exists(Constants.TTSAlertFilePath) && !disableTTSalert) {
				Helpers.ExecuteCommand($"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.ResourcesDirectory} && play {Constants.TTSAlertFileName} -q", false);				
			}

			byte[] result;
			switch (context) {
				case SpeechContext.AssistantStartup:
					if (!File.Exists(Constants.StartupSpeechFilePath)) {
						Logger.Log($"{Core.AssistantName} startup tts sound doesn't exist, downloading the sound...", LogLevels.Trace);

						result = Helpers.GetUrlToBytes($"http://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&q={text}&tl=En-us", Method.GET, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36");
						Logger.Log("Fetched voice file bytes.", LogLevels.Trace);

						if (result.Length <= 0 || result == null) {
							Logger.Log("result returned as null!", LogLevels.Error);
							return;
						}

						Logger.Log($"Writting to file => {Constants.StartupSpeechFilePath}", LogLevels.Trace);
						Helpers.WriteBytesToFile(result, Constants.StartupSpeechFilePath);
					}

					if (File.Exists(Constants.StartupSpeechFilePath)) {
						Task.Delay(500).Wait();
						Helpers.ExecuteCommand($"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.TextToSpeechDirectory} && play {Constants.StartupFileName} -q", false);
					}
					else {
						Logger.Log("An error occured, either download failed, or the file doesn't exist!", LogLevels.Error);
						return;
					}
					break;
				case SpeechContext.NewEmaiNotification:
					if (!File.Exists(Constants.NewMailSpeechFilePath)) {
						Logger.Log($"{Core.AssistantName} startup tts sound doesn't exist, downloading the sound...", LogLevels.Trace);

						result = Helpers.GetUrlToBytes($"http://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&q={text}&tl=En-us", Method.GET, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36");
						Logger.Log("Fetched voice file bytes.", LogLevels.Trace);

						if (result.Length <= 0 || result == null) {
							Logger.Log("result returned as null!", LogLevels.Error);
							return;
						}

						Logger.Log($"Writting to file => {Constants.NewMailSpeechFilePath}", LogLevels.Trace);
						Helpers.WriteBytesToFile(result, Constants.NewMailSpeechFilePath);
					}

					if (File.Exists(Constants.NewMailSpeechFilePath)) {
						Helpers.ExecuteCommand($"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.TextToSpeechDirectory} && play {Constants.NewMailFileName} -q", false);
					}
					else {
						Logger.Log("An error occured, either download failed, or the file doesn't exist!", LogLevels.Error);
						return;
					}
					break;
				case SpeechContext.Custom:
					result = Helpers.GetUrlToBytes($"http://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&q={text}&tl=En-us", Method.GET, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36");
					Logger.Log("Fetched voice file bytes.", LogLevels.Trace);

					if (result.Length <= 0 || result == null) {
						Logger.Log("result returned as null!", LogLevels.Error);
						return;
					}

					string fileName = $"{DateTime.Now.Ticks}.mp3";

					Logger.Log($"Writting to file => {fileName}", LogLevels.Trace);
					Helpers.WriteBytesToFile(result, Constants.TextToSpeechDirectory + "/" + fileName);
					Task.Delay(200).Wait();
					if (File.Exists(Constants.TextToSpeechDirectory + "/" + fileName)) {
						Helpers.ExecuteCommand($"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.TextToSpeechDirectory} && play {fileName} -q", false);
						Logger.Log($"Played the file {fileName} sucessfully", LogLevels.Trace);
					}
					else {
						Logger.Log("An error occured, either download failed, or the file doesn't exist!", LogLevels.Error);
						return;
					}
					break;
			}
		}
	}
}
