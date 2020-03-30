using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Computer;
using static Assistant.Gpio.Enums;

namespace Assistant.Gpio.Controllers {
	public class SoundController {
		private readonly ILogger Logger = new Logger(typeof(SoundController).Name);
		private bool IsAllowedToRun => IOController.GetDriver() != null && IOController.GetDriver()?.DriverName == EGPIO_DRIVERS.RaspberryIODriver;

		public async Task SetAudioState(PiAudioState state) {
			if (!IsAllowedToRun) {
				return;
			}

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

		public async Task<AudioState> GetAudioState() {
			if (!IsAllowedToRun) {
				return default;
			}

			return await Pi.Audio.GetState().ConfigureAwait(false);
		}

		public async Task SetVolume(int level = 80) {
			if (!IsAllowedToRun) {
				return;
			}

			await Pi.Audio.SetVolumePercentage(level).ConfigureAwait(false);
		}

		public async Task SetVolume(float decibels = -1.00f) {
			if (!IsAllowedToRun) {
				return;
			}

			await Pi.Audio.SetVolumeByDecibels(decibels).ConfigureAwait(false);
		}
	}
}
