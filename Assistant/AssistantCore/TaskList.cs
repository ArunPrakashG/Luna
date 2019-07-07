using HomeAssistant.Extensions;
using HomeAssistant.Log;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HomeAssistant.AssistantCore {

	public class TaskStructure<T> where T : Task {

		public float TaskIdentifier { get; set; }

		public string TaskMessage { get; set; }

		public bool LongRunning { get; set; }

		public TimeSpan Delay { get; set; }

		public DateTime TimeAdded { get; set; }

		public T Task { get; set; }
	}

	public class TaskList {

		public List<TaskStructure<Task>> TaskFactoryCollection { get; private set; } = new List<TaskStructure<Task>>();
		private readonly Logger Logger = new Logger("TASKS");
		private bool CancelTaskListener = false;

		public void TryAddTask(TaskStructure<Task> task) {
			if (task == null) {
				Logger.Log("Task is null.", Enums.LogLevels.Warn);
				return;
			}

			TaskFactoryCollection.Add(task);
			OnTaskAdded(TaskFactoryCollection.IndexOf(task));
			Logger.Log("Task added sucessfully.", Enums.LogLevels.Trace);
		}

		public TaskStructure<Task> TryRemoveTask(float taskId) {
			TaskStructure<Task> cachedTaskStruct = null;

			if (TaskFactoryCollection.Count <= 0) {
				return cachedTaskStruct;
			}

			foreach (TaskStructure<Task> task in TaskFactoryCollection) {
				if (task.TaskIdentifier.Equals(taskId)) {
					cachedTaskStruct = task;
					TaskFactoryCollection.Remove(task);
					Logger.Log($"A task with identifier {taskId} and message {task.TaskMessage} has been removed from the queue.", Enums.LogLevels.Trace);
					OnTaskRemoved(cachedTaskStruct);
					return cachedTaskStruct;
				}
			}

			return cachedTaskStruct;
		}

		private void OnTaskAdded(int index) {
			if (TaskFactoryCollection.Count <= 0) {
				return;
			}

			TaskStructure<Task> item = TaskFactoryCollection[index];

			if (item == null) {
				return;
			}

			if (item.TimeAdded.AddMilliseconds(item.Delay.Milliseconds) <= DateTime.Now) {
				TimeSpan delay = DateTime.Now.Subtract(item.TimeAdded.AddMilliseconds(item.Delay.Milliseconds));
				Logger.Log($"TASK > {item.TaskMessage} will be executed {delay.Hours}/{delay.Minutes}/{delay.Seconds} (hr/min/sec) from now. ({item.TaskIdentifier})");
				Helpers.ScheduleTask(() => item.Task, delay, item.LongRunning);
			}
			else {
				TryRemoveTask(item.TaskIdentifier);
				Logger.Log($"Cannot execute TASK > {item.TaskMessage} as the delay time is over or not set correctly. Removed the task.", Enums.LogLevels.Warn);
			}
		}

		private void OnTaskRemoved(TaskStructure<Task> item) {
		}

		public void StopTaskListener() => CancelTaskListener = true;

		private void TaskChangeListerner() {
			Helpers.InBackgroundThread(() => {
				int taskCount = TaskFactoryCollection.Count;
				while (true) {
					if (taskCount < TaskFactoryCollection.Count) {
						taskCount = TaskFactoryCollection.Count;
						Logger.Log("A task has been added.", Enums.LogLevels.Trace);

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
