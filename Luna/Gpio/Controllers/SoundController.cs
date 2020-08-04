using Luna.CommandLine;
using Luna.Logging;
using System;
using static Luna.Gpio.Enums;

namespace Luna.Gpio.Controllers {
	internal class SoundController : IDisposable {
		private readonly InternalLogger Logger = new InternalLogger(nameof(SoundController));
		private readonly GpioCore Controller;
		private readonly aMixerCommandInterfacer aMixerInterfacer;

		internal SoundController(GpioCore gpioCore) {
			Controller = gpioCore;
			aMixerInterfacer = new aMixerCommandInterfacer(false, false, false);
		}

		internal void SetAudioState(AudioState state) {
			switch (state) {
				case AudioState.Mute:
					aMixerInterfacer.SetVolumn(0);
					Logger.Info("Pi audio is muted.");
					break;

				case AudioState.Unmute:
					aMixerInterfacer.SetVolumn(60);
					Logger.Info("Pi audio is Unmuted.");
					break;
			}
		}

		internal void SetVolume(int level = 80) => aMixerInterfacer.SetVolumn(0);

		public void Dispose() => aMixerInterfacer.Dispose();
	}
}
