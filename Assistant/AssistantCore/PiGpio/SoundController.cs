using Assistant.Log;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Computer;

namespace Assistant.AssistantCore.PiGpio {
	public class SoundController {

		private readonly Logger Logger = new Logger("PI-SOUND");

		public async Task SetPiAudioState(Enums.PiAudioState state) {
			switch (state) {
				case Enums.PiAudioState.Mute:
					await Pi.Audio.ToggleMute(true).ConfigureAwait(false);
					Logger.Log("pi audio is muted.");
					break;

				case Enums.PiAudioState.Unmute:
					await Pi.Audio.ToggleMute(false).ConfigureAwait(false);
					Logger.Log("pi audio is unmuted.");
					break;
			}
		}

		public async Task<AudioState> GetAudioState() => await Pi.Audio.GetState().ConfigureAwait(false);

		public async Task SetPiVolume(int level = 80) => await Pi.Audio.SetVolumePercentage(level).ConfigureAwait(false);

		public async Task SetPiVolume(float decibels = -1.00f) => await Pi.Audio.SetVolumeByDecibels(decibels).ConfigureAwait(false);
	}
}
