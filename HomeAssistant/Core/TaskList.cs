using HomeAssistant.Extensions;
using HomeAssistant.Log;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Core {
	public class TaskStructure<T> where T : Task {
		public string TaskIdentifier { get; set; }
		public string TaskMessage { get; set; }
		public bool IsAlreadyCompleted { get; set; }
		public bool LongRunning { get; set; }
		public DateTime TimeAdded { get; set; }
		public DateTime TimeEnding { get; set; }
		public T Task { get; set; }
	}

	public class TaskList {

		public ConcurrentQueue<TaskStructure<Task>> ConcurrentTasks = new ConcurrentQueue<TaskStructure<Task>>();
		private readonly Logger Logger = new Logger("TASKS");
		private bool CancelTaskListener = false;

		public void TryEnqueue(TaskStructure<Task> task) {
			if (task == null) {
				Logger.Log("Task is null.", LogLevels.Warn);
				return;
			}

			ConcurrentTasks.Enqueue(task);
			Helpers.InBackground(() => OnEnqueued(task));
			Logger.Log("Task added sucessfully.", LogLevels.Trace);
		}

		public TaskStructure<Task> TryDequeue() {
			bool result = ConcurrentTasks.TryDequeue(out TaskStructure<Task> task);
			if (!result) {
				Logger.Log("Failed to fetch from the queue.", LogLevels.Error);
				return null;
			}

			Logger.Log("Fetching task sucessfully!", LogLevels.Trace);
			Helpers.InBackground(() => OnDequeued(task));
			return task;
		}

		private void OnEnqueued(TaskStructure<Task> item) {
			if (item.IsAlreadyCompleted) {
				return;
			}

			if (DateTime.Now == item.TimeEnding) {
				Helpers.InBackground(() => item.Task, item.LongRunning);
			}
			else {
				long delay = (item.TimeEnding - DateTime.Now).Ticks;
				TimeSpan delaySpan = new TimeSpan(delay);
				Helpers.ScheduleTask(() => item.Task, delaySpan);
			}
		}

		private void OnDequeued(TaskStructure<Task> item) {

		}

		public void StopTaskListener() => CancelTaskListener = true;

		private void TaskChangeListerner() {
			Helpers.InBackgroundThread(() => {
				int taskCount = ConcurrentTasks.Count;
				while (true) {
					if (taskCount < ConcurrentTasks.Count) {
						taskCount = ConcurrentTasks.Count;
						Logger.Log("A task has been added.", LogLevels.Trace);
						//TODO: On task added
					}

					if (CancelTaskListener) {
						break;
					}

					Task.Delay(1).Wait();
				}
			}, "Task queue listener");
		}
	}
}
