using Assistant.Extensions;
using Assistant.Log;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Assistant.AssistantCore {

	public class TaskStructure {

		[Required]
		public float TaskIdentifier { get; set; }

		[Required]
		public bool LongRunning { get; set; }

		[Required]
		public DateTime ExecutionTime { get; set; }

		[Required]
		public Task? Task { get; set; }
	}

	public class TaskScheduler {
		private List<TaskStructure> TaskFactoryCollection { get; set; } = new List<TaskStructure>();
		private readonly Logger Logger = new Logger("TASKS");
		private TaskStructure? PreviousRemovedTask { get; set; }

		public bool IsTaskCollectionEmpty => TaskFactoryCollection.Count <= 0;

		public void OnCoreShutdownRequested() {
			if (!IsTaskCollectionEmpty) {
				foreach (TaskStructure task in TaskFactoryCollection) {
					Logger.Log($"TASK > {task.TaskIdentifier} execution cancelled as shutdown was requested.", Enums.LogLevels.Warn);
				}
			}
		}

		public (bool, Task?) TryAddTask(TaskStructure task) {
			if (task == null) {
				Logger.Log("Task is null.", Enums.LogLevels.Warn);
				return (false, null);
			}

			if (TaskFactoryCollection.Count > 0) {
				foreach (TaskStructure t in TaskFactoryCollection) {
					if (t.TaskIdentifier.Equals(task.TaskIdentifier)) {
						Logger.Log("Such a task already exists. cannot add again!", Enums.LogLevels.Warn);
						return (false, t.Task);
					}
				}
			}

			TaskFactoryCollection.Add(task);
			OnTaskAdded(TaskFactoryCollection.IndexOf(task));
			Logger.Log("Task added.", Enums.LogLevels.Trace);
			return (true, task.Task);
		}

		public (bool, Task?) TryAddTask(TaskStructure task, bool removeIfExists) {
			if (task == null) {
				Logger.Log("Task is null.", Enums.LogLevels.Warn);
				return (false, null);
			}

			if (TaskFactoryCollection.Count > 0) {
				foreach (TaskStructure t in TaskFactoryCollection) {
					if (t.TaskIdentifier.Equals(task.TaskIdentifier)) {
						if (removeIfExists) {
							TryRemoveTask(t.TaskIdentifier);
						}
						Logger.Log("Such a task already exists. Removed the task.", Enums.LogLevels.Warn);
						return (true, t.Task);
					}
				}
			}

			TaskFactoryCollection.Add(task);
			OnTaskAdded(TaskFactoryCollection.IndexOf(task));
			Logger.Log("Task added.", Enums.LogLevels.Trace);
			return (true, task.Task);
		}

		public bool TryRemoveTask(float taskId) {
			if (TaskFactoryCollection.Count <= 0) {
				return false;
			}

			foreach (TaskStructure task in TaskFactoryCollection) {
				if (task.TaskIdentifier.Equals(taskId)) {
					PreviousRemovedTask = task;
					TaskFactoryCollection.Remove(task);
					Logger.Log($"task with identifier {taskId} has been removed from the queue.", Enums.LogLevels.Trace);
					OnTaskRemoved(PreviousRemovedTask);
					return true;
				}
			}

			return false;
		}

		private void OnTaskAdded(int index) {
			if (TaskFactoryCollection.Count <= 0) {
				return;
			}

			TaskStructure item = TaskFactoryCollection[index];

			if (item == null) {
				return;
			}

			if (item.ExecutionTime < DateTime.Now) {
				Logger.Log($"TASK >> {item.TaskIdentifier} is out of its execution time.", Enums.LogLevels.Warn);
				Logger.Log($"TASK >> Removing task {item.TaskIdentifier}");
				TryRemoveTask(item.TaskIdentifier);
				return;
			}

			double taskExecutionSpan = (item.ExecutionTime - DateTime.Now).TotalSeconds;
			Helpers.ScheduleTask(item, TimeSpan.FromSeconds(taskExecutionSpan), item.LongRunning);
			Logger.Log($"TASK >>> {item.TaskIdentifier} will be executed in {TimeSpan.FromSeconds(taskExecutionSpan).TotalMinutes} minutes from now.");
		}

		private void OnTaskRemoved(TaskStructure item) {
		}
	}
}
