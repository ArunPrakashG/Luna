using Luna.Logging;
using Synergy.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Luna {
	internal static class Notifications {
		private static readonly InternalLogger Logger = new InternalLogger(nameof(Notifications));
		private static bool MuteNotifications = false;

		internal static void Mute() => MuteNotifications = true;

		internal static void Unmute() => MuteNotifications = false;

		internal static void Notify() {
			if (MuteNotifications) {
				return;
			}

			// we use bash command
			// will require command line player to be installed
			if (OS.IsUnix) {

			}

			// we use media player api
			if(Helpers.GetPlatform() == OSPlatform.Windows) {

			}
		}
	}
}
