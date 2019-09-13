using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Assistant.Remainders {
	public class RemainderManager {
		public List<(Remainder, Timer)> RemainderCollection { get; private set; } = new List<(Remainder, Timer)>();
		private Logger Logger = new Logger("REMAINDER");

		public bool Remind(string msgToRemind, int minsUntilReminding) {
			if (Helpers.IsNullOrEmpty(msgToRemind) || minsUntilReminding <= 0) {
				return false;
			}

			double hrs = TimeSpan.FromMinutes(minsUntilReminding).TotalHours;

			(Remainder, Timer) remainderData = (new Remainder {
				IsCompleted = false,
				Message = msgToRemind,
				RemaindAt = DateTime.Now.AddHours(hrs),
				UniqueId = Guid.NewGuid().ToString()
			}, null);

			if (!RemainderCollection.Exists(x => x.Item1.Message.Equals(msgToRemind, StringComparison.OrdinalIgnoreCase))) {
				RemainderCollection.Add(remainderData);
			}

			return SetRemainder(remainderData);
		}

		private bool SetRemainder((Remainder, Timer) remainderData) {
			if (remainderData.Item1 == null) {
				return false;
			}

			if (remainderData.Item1.IsCompleted || remainderData.Item1.RemaindAt > DateTime.Now) {
				return true;
			}

			Timer timer = Helpers.ScheduleTask(async () => {
				Logger.Log($"REMAINDER >>> {remainderData.Item1.Message}", Enums.LogLevels.Success);
				await TTSService.SpeakText(remainderData.Item1.Message, true).ConfigureAwait(false);

			}, TimeSpan.FromMinutes((remainderData.Item1.RemaindAt - DateTime.Now).TotalMinutes));

			foreach ((Remainder, Timer) t in RemainderCollection) {
				if (t.Item1.UniqueId == remainderData.Item1.UniqueId) {
					RemainderCollection[RemainderCollection.IndexOf(t)] = (remainderData.Item1, timer);
				}
			}

			return true;
		}
	}
}
