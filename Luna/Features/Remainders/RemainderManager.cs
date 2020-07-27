using Luna.Logging;
using Luna.Logging.Interfaces;
using Luna.Sound.Speech;
using FluentScheduler;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Luna.Logging.Enums;

namespace Luna.Features.Remainders {
	public static class RemainderManager {
		public static readonly Dictionary<string, Remainder> Remainders = new Dictionary<string, Remainder>();
		private static readonly ILogger Logger = new Logger(typeof(RemainderManager).Name);

		public static bool Remind(Remainder obj) {
			if (obj == null || string.IsNullOrEmpty(obj.Message) || string.IsNullOrEmpty(obj.UniqueId) ||
				obj.RemaindAt < DateTime.Now) {
				return false;
			}

			try {
				JobManager.AddJob(async () => await OnJobExecAsync(obj).ConfigureAwait(false), (s) => s.WithName(obj.UniqueId).ToRunOnceAt(obj.RemaindAt));
				Remainders.Add(obj.UniqueId, obj);
				Schedule sch = JobManager.GetSchedule(obj.UniqueId);

				if (sch == null) {
					return false;
				}

				Logger.Info($"A remainder has been set at {sch.NextRun.ToString()} ({Math.Round((sch.NextRun - DateTime.Now).TotalMinutes, 3)} minutes left)");
				return !sch.Disabled;
			}
			catch (Exception e) {
				Logger.Exception(e);
				return false;
			}
		}

		private static async Task OnJobExecAsync(Remainder r) {
			if (r == null || string.IsNullOrEmpty(r.Message) || r.RemaindAt < DateTime.Now || r.Func == null) {
				return;
			}

			Logger.Log($"REMAINDER >>> {r.Message}", LogLevels.Green);
			await TTS.SpeakText("Sir, You have a remainder!", true).ConfigureAwait(false);
			await Task.Delay(400).ConfigureAwait(false);
			await TTS.SpeakText(r.Message, false).ConfigureAwait(false);

			if (r.Func != null) {
				r.Func.Invoke(r);
			}

			if (string.IsNullOrEmpty(r.UniqueId)) {
				return;
			}

			try {
				if (Remainders.ContainsKey(r.UniqueId)) {
					Remainders.Remove(r.UniqueId);
				}
			}
			catch (Exception e) {
				Logger.Exception(e);
				return;
			}
		}
	}
}
