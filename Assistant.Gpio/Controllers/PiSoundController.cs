using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Computer;
using static Assistant.Gpio.Controllers.PiController;

namespace Assistant.Gpio.Controllers {
	public class PiSoundController {
		private readonly ILogger Logger = new Logger(typeof(PiSoundController).Name);

		public async Task SetPiAudioState(PiAudioState state) {
			switch (state) {
				case PiAudioState.Mute:
					await Pi.Audio.ToggleMute(true).ConfigureAwait(false);
					Logger.Log("Pi audio is muted.");
					break;

				case PiAudioState.Unmute:
					await Pi.Audio.ToggleMute(false).ConfigureAwait(false);
					Logger.Log("Pi audio is Unmuted.");
					break;
			}
		}

		public async Task<AudioState> GetAudioState() => await Pi.Audio.GetState().ConfigureAwait(false);

		public async Task SetPiVolume(int level = 80) => await Pi.Audio.SetVolumePercentage(level).ConfigureAwait(false);

		public async Task SetPiVolume(float decibels = -1.00f) => await Pi.Audio.SetVolumeByDecibels(decibels).ConfigureAwait(false);
	}
}
