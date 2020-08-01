using Luna.CommandLine.ProcessBase;
using Luna.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Luna.CommandLine {
	internal class FestivalCommandSession : SessionizedProcess {
		private const string SessionInitiatorCommand = "festival";
		private static readonly OSPlatform SupportedPlatform = OSPlatform.Linux;

		internal FestivalCommandSession(bool ioLogging = false, bool asAdmin = false) : base(SupportedPlatform, SessionInitiatorCommand, ioLogging, asAdmin) {	}

		private string GenerateSayTextCommand(string text) {
			if (string.IsNullOrEmpty(text)) {
				return null;
			}

			return string.Format("({0} \"{1}\")", "SayText", text);
		}

		private string GenerateTTSCommand(string fileName) {
			if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName)) {
				return null;
			}

			return string.Format("({0} \"{1}\" nil)", "tts", fileName);
		}

		private string GenerateExitCommand() => string.Format("({0})", "quit");

		private string GenerateSelectVoiceCommand(FestivalVoice voice) {
			switch (voice) {
				case FestivalVoice.Kal:
					return string.Format("({0})", "voice_kal_diphone");					
				case FestivalVoice.Rab:
					return string.Format("({0})", "voice_rab_diphone");
				case FestivalVoice.CmuArticHTS:
					return "echo \"(set! voice_default 'voice_cmu_us_slt_arctic_hts)\" | sudo tee - a / etc / festival.scm";
				default:
					goto case FestivalVoice.Kal;
			}
		}

		internal void SayText(string text) {
			if (string.IsNullOrEmpty(text)) {
				return;
			}

			WriteLine(GenerateSayTextCommand(text));
		}

		internal void TTSFromFile(string fileName) {
			if(string.IsNullOrEmpty(fileName) || !File.Exists(fileName)) {
				return;
			}

			WriteLine(GenerateTTSCommand(fileName));
		}

		internal void ExitSession() => WriteLine(GenerateExitCommand());

		internal void SetVoice(FestivalVoice voice) => WriteLine(GenerateSelectVoiceCommand(voice));

		internal enum FestivalVoice {
			Rab,
			Kal,
			CmuArticHTS
		}

		protected override void ProcessStandardError(object sender, NotifyCollectionChangedEventArgs e) {
			if (!ErrorContainer.TryPop(out string? newLine)) {
				return;
			}

			if (EnableIOLogging) {
				ProcessLog(newLine, ProcessLogLevel.Error);
			}
		}

		protected override void ProcessStandardInput(object sender, NotifyCollectionChangedEventArgs e) {
			if (!OutputContainer.TryPop(out string? newLine)) {
				return;
			}

			if (EnableIOLogging) {
				ProcessLog(newLine, ProcessLogLevel.Info);
			}
		}

		protected override void ProcessStandardOutput(object sender, NotifyCollectionChangedEventArgs e) {
			if (!InputContainer.TryPop(out string? newLine)) {
				return;
			}

			if (EnableIOLogging) {
				ProcessLog(newLine, ProcessLogLevel.Input);
			}
		}
	}
}
