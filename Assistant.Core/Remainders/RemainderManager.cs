using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Assistant.Remainders {
	public class RemainderManager {
		public List<(Remainder, Timer?)> RemainderCollection { get; private set; } = new List<(Remainder, Timer?)>();
		private Logger Logger = new Logger("REMAINDER");

		public bool Remind(string msgToRemind, int minsUntilReminding) {
			if (string.IsNullOrEmpty(msgToRemind) || minsUntilReminding <= 0) {
				return false;
			}

			(Remainder, Timer?) remainderData = (new Remainder {
				IsCompleted = false,
				Message = msgToRemind,
				RemaindAt = DateTime.Now.AddMinutes(minsUntilReminding),
				UniqueId = Guid.NewGuid().ToString()
			}, null);

			if (!RemainderCollection.Exists(x => x.Item1.Message != null && x.Item1.Message.Equals(msgToRemind, StringComparison.OrdinalIgnoreCase))) {
				RemainderCollection.Add(remainderData);
			}

			return SetRemainder(remainderData);
		}

		private bool SetRemainder((Remainder, Timer?) remainderData) {
			if (remainderData.Item1 == null) {
				return false;
			}

			if (remainderData.Item1.IsCompleted || remainderData.Item1.RemaindAt < DateTime.Now) {
				return true;
			}

			Timer? timer = Helpers.ScheduleTask(async () => {
				Logger.Log($"REMAINDER >>> {remainderData.Item1.Message}", Enums.LogLevels.Success);
				await TTS.SpeakText("Sir, You have a remainder!", true).ConfigureAwait(false);
				if (remainderData.Item1.Message != null && !remainderData.Item1.Message.IsNull()) {
					await TTS.SpeakText(remainderData.Item1.Message, false).ConfigureAwait(false);
				}

			}, TimeSpan.FromMinutes((remainderData.Item1.RemaindAt - DateTime.Now).TotalMinutes));

			Logger.Log($"A remainder has been set for message: {remainderData.Item1.Message} from {(remainderData.Item1.RemaindAt - DateTime.Now).Minutes} minutes from now.");

			foreach ((Remainder, Timer?) t in RemainderCollection) {
				if (t.Item2 == null) {
					continue;
				}
				if (t.Item1.UniqueId == remainderData.Item1.UniqueId) {
					RemainderCollection[RemainderCollection.IndexOf(t)] = (remainderData.Item1, timer);
					break;
				}
			}

			return true;
		}
	}
}
