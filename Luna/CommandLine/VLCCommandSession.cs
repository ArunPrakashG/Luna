using Luna.CommandLine.ProcessBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Luna.CommandLine {
	internal class VLCCommandSession : SessionizedProcess {
		private const string SessionInitiatorCommand = "vlc";
		private static readonly OSPlatform SupportedPlatform = OSPlatform.Linux;

		internal VLCCommandSession(bool ioLogging = false, bool asAdmin = false) : base(SupportedPlatform, SessionInitiatorCommand, ioLogging, asAdmin) { }

		internal void AddSong(string songFilePath) {
			if (string.IsNullOrEmpty(songFilePath) || !File.Exists(songFilePath)) {
				return;
			}

			WriteLine(GenerateAddSongCommand(songFilePath));
		}

		internal void EnqueueSong(string songFilePath) {
			if (string.IsNullOrEmpty(songFilePath) || !File.Exists(songFilePath)) {
				return;
			}

			WriteLine(GenerateEnqueueSongCommand(songFilePath));
		}

		internal void Play() => WriteLine(GeneratePlaySongCommand());

		internal void Stop() => WriteLine(GenerateStopSongCommand());

		internal void Next() => WriteLine(GenerateNextSongCommand());

		internal void Previous() => WriteLine(GeneratePreviousSongCommand());

		internal void Clear() => WriteLine(GenerateClearCommand());

		internal void Pause() => WriteLine(GeneratePauseSongCommand());

		internal void Shutdown() => WriteLine(GenerateShutdownCommand());

		private string GenerateAddSongCommand(string songFilePath) {
			if(string.IsNullOrEmpty(songFilePath) || !File.Exists(songFilePath)) {
				return null;
			}

			return string.Format("{0} {1}", "add", songFilePath);
		}

		private string GenerateEnqueueSongCommand(string songFilePath) {
			if (string.IsNullOrEmpty(songFilePath) || !File.Exists(songFilePath)) {
				return null;
			}

			return string.Format("{0} {1}", "enqueue", songFilePath);
		}

		private string GeneratePlaySongCommand() => string.Format("{0}", "play");

		private string GenerateStopSongCommand() => string.Format("{0}", "stop");

		private string GenerateNextSongCommand() => string.Format("{0}", "next");

		private string GeneratePreviousSongCommand() => string.Format("{0}", "prev");

		private string GenerateClearCommand() => string.Format("{0}", "clear");

		private string GeneratePauseSongCommand() => string.Format("{0}", "pause");

		private string GenerateShutdownCommand() => string.Format("{0}", "shutdown");
	}
}
