using Google.Cloud.Speech.V1;
using HomeAssistant.Core;
using HomeAssistant.Extensions;
using HomeAssistant.Log;
using RestSharp;
using System;
using System.IO;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Modules {

	public class GoogleSpeech {
		private Logger Logger = new Logger("GOOGLE-SPEECH");

		public GoogleSpeech() {
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

		public void SpeakText(string text) {
			byte[] result = Helpers.GetUrlToBytes($"http://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&q={text}&tl=En-us", Method.GET, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36");
			Logger.Log("Fetched voice file bytes.", LogLevels.Trace);
			if(result.Length <= 0 || result == null) {
				Logger.Log("result returned as null!", LogLevels.Error);
				return;
			}

			if (!Directory.Exists(Constants.TextToSpeechDirectory)) {
				Directory.CreateDirectory(Constants.TextToSpeechDirectory);
			}

			long fileTime = DateTime.Now.ToBinary();
			Logger.Log("Writting to file.", LogLevels.Trace);
			Helpers.WriteBytesToFile(result, $"{Constants.TextToSpeechDirectory}/TTS_{fileTime}.mp3");

			if (File.Exists($"{Constants.TextToSpeechDirectory}/TTS_{fileTime}.mp3")) {
				Helpers.ExecuteCommand($"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.TextToSpeechDirectory} && play TTS_{fileTime}.mp3", Tess.Config.Debug ? true : false);
			}
			else {
				Logger.Log("File not found, possibly, failed to download.", LogLevels.Error);
			}
		}
	}
}
