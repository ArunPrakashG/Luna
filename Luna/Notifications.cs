using Luna.CommandLine;
using Luna.Features;
using Luna.Logging;
using Luna.Properties;
using Synergy.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Luna {
	internal static class Notifications {
		private const int TempRemoverDelay = 1; // in hours
		private static readonly InternalLogger Logger = new InternalLogger(nameof(Notifications));
		private static bool MuteNotifications = false;
		private static readonly PeriodicTempRemover TempRemover;

		static Notifications() {
			TempRemover = new PeriodicTempRemover(TimeSpan.FromHours(TempRemoverDelay));
		}

		internal static void Mute() => MuteNotifications = true;

		internal static void Unmute() => MuteNotifications = false;

		internal static void Notify(NotificationType type) {
			if (MuteNotifications) {
				return;
			}

			// we use bash command
			// will require command line player such as sox/vlc to be installed
			if (Helpers.GetPlatform() == OSPlatform.Linux || Helpers.GetPlatform() == OSPlatform.FreeBSD) {
				NotifyUnix(type);
				return;
			}

			// we use windows media player api
			if(Helpers.GetPlatform() == OSPlatform.Windows) {
				NotifyWindows(type);
				return;
			}

			// if its mac os, i apologise
			// sorry i have 0 knowledge with mac systems :'(			
		}

		private static void NotifyUnix(NotificationType type) {
			switch (type) {
				case NotificationType.NotifyGeneric:					
					using (SoxCommandInterfacer sox = new SoxCommandInterfacer(false, true, false)) {
						sox.Play(WriteToTempPath(Resources.NotificationGeneric));
					}

					break;
				case NotificationType.NotifyLong:
					using (SoxCommandInterfacer sox = new SoxCommandInterfacer(false, true, false)) {
						sox.Play(WriteToTempPath(Resources.NotificationLong));
					}

					break;
				case NotificationType.NotifyShort:
					using (SoxCommandInterfacer sox = new SoxCommandInterfacer(false, true, false)) {
						sox.Play(WriteToTempPath(Resources.NotificationShort));
					}

					break;
				case NotificationType.NotifyMail:
					using (SoxCommandInterfacer sox = new SoxCommandInterfacer(false, true, false)) {
						sox.Play(WriteToTempPath(Resources.NotificationMail));
					}

					break;
			}
		}

		private static void NotifyWindows(NotificationType type) {
			switch (type) {
				case NotificationType.NotifyGeneric:
					using (SoundPlayer player = new SoundPlayer(WriteToTempPath(Resources.NotificationGeneric))) {
						player.Play();
					}

					break;
				case NotificationType.NotifyLong:
					using (SoundPlayer player = new SoundPlayer(WriteToTempPath(Resources.NotificationLong))) {
						player.Play();
					}

					break;
				case NotificationType.NotifyShort:
					using (SoundPlayer player = new SoundPlayer(WriteToTempPath(Resources.NotificationShort))) {
						player.Play();
					}

					break;
				case NotificationType.NotifyMail:
					using (SoundPlayer player = new SoundPlayer(WriteToTempPath(Resources.NotificationMail))) {
						player.Play();
					}

					break;
			}
		}

		private static string? WriteToTempPath(byte[] bytes) {
			if(bytes.Length <= 0) {
				return null;
			}

			string writePath = Path.Combine(Path.GetTempPath(), $"{new Guid(bytes).ToString("N")}" + ".mp3");
			File.WriteAllBytes(writePath, bytes);
			return File.Exists(writePath) ? writePath : null;
		}		

		internal enum NotificationType {
			NotifyLong,
			NotifyShort,
			NotifyGeneric,
			NotifyMail
		}
	}
}
