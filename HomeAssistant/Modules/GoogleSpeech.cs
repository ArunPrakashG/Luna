using Google.Cloud.Speech.V1;
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

		private void Init(string filePath) {
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
			if (!Directory.Exists(Constants.TextToSpeechDirectory)) {
				Directory.CreateDirectory(Constants.TextToSpeechDirectory);
			}

			long fileTime = DateTime.Now.ToBinary();
			Helpers.WriteBytesToFile(result, $"{Constants.TextToSpeechDirectory}/TTS_{fileTime}");

			if (File.Exists($"{Constants.TextToSpeechDirectory}/TTS_{fileTime}")) {
				Helpers.ExecuteCommand($"cd /home/pi/Desktop/HomeAssistant/AssistantCore/{Constants.TextToSpeechDirectory} && play TTS_{fileTime}", true);
			}
			else {
				Logger.Log("An error as occured, couldnt play the file.", LogLevels.Error);
			}
		}
	}
}
