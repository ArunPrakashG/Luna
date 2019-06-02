using Google.Cloud.Speech.V1;
using HomeAssistant.Log;
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
	}
}
