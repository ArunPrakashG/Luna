using HomeAssistant.Extensions;
using HomeAssistant.Log;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Core {
	public class TaskStructure {
		[JsonProperty]
		public string TaskIdentifier { get; set; }
		[JsonProperty]
		public string TaskMessage { get; set; }
		[JsonProperty]
		public bool IsAlreadyCompleted { get; set; }
		[JsonProperty]
		public DateTime TimeAdded { get; set; }
		[JsonProperty]
		public DateTime TimeEnding { get; set; }
		[JsonProperty]
		public object TaskData { get; set; }
	}

	public class TaskQueueRoot {
		[JsonProperty]
		public ConcurrentQueue<TaskStructure> TaskList { get; set; }
	}

	public class TaskList {

		private TaskQueueRoot TaskRootObject;
		public ConcurrentQueue<TaskStructure> ConcurrentTasks = new ConcurrentQueue<TaskStructure>();

		private readonly Logger Logger = new Logger("TASKS");
		private bool CancelTaskListener = false;

		private void TryEnqueue(TaskStructure task) {
			if (task == null) {
				Logger.Log("Task is null.", LogLevels.Warn);
				return;
			}

			ConcurrentTasks.Enqueue(task);
			Helpers.InBackground(() => OnEnqueued(task));
			Logger.Log("Task added sucessfully.", LogLevels.Trace);
		}

		private TaskStructure TryDequeue() {
			bool result = ConcurrentTasks.TryDequeue(out TaskStructure task);

			if (!result) {
				Logger.Log("Failed to fetch from the queue.", LogLevels.Error);
				return null;
			}

			Logger.Log("Fetching task sucessfully!", LogLevels.Trace);
			Helpers.InBackground(() => OnDequeued(task));
			return task;
		}

		private void OnEnqueued(TaskStructure item) {

		}

		private void OnDequeued(TaskStructure item) {

		}

		public void SaveGPIOConfig(GPIOConfigRoot Config) {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config folder doesn't exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			JsonSerializer serializer = new JsonSerializer();
			JsonConvert.SerializeObject(Config, Formatting.Indented);
			string pathName = Constants.TaskQueueFilePath;
			using (StreamWriter sw = new StreamWriter(pathName, false)) {
				using (JsonWriter writer = new JsonTextWriter(sw)) {
					writer.Formatting = Formatting.Indented;
					serializer.Serialize(writer, Config);
					Logger.Log("Updated task config!");
					sw.Dispose();
				}
			}
		}

		public TaskQueueRoot LoadConfig() {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Such a folder doesn't exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			if (!File.Exists(Constants.TaskQueueFilePath)) {
				bool loaded = GenerateDefaultConfig();
				if (!loaded) {
					return null;
				}
			}

			string JSON = null;
			using (FileStream Stream = new FileStream(Constants.TaskQueueFilePath, FileMode.Open, FileAccess.Read)) {
				using (StreamReader ReadSettings = new StreamReader(Stream)) {
					JSON = ReadSettings.ReadToEnd();
				}
			}

			TaskRootObject = JsonConvert.DeserializeObject<TaskQueueRoot>(JSON);
			ConcurrentTasks = TaskRootObject.TaskList;
			Logger.Log("Tasks loaded sucessfully!");
			return TaskRootObject;
		}

		public bool GenerateDefaultConfig() {
			Logger.Log("Tasks file doesnt exist. press c to continue generating default config or q to quit.");

			ConsoleKeyInfo? Key = Helpers.FetchUserInputSingleChar(TimeSpan.FromMinutes(1));

			if (!Key.HasValue) {
				Logger.Log("No value has been entered, continuing to run the program...");
			}
			else {
				switch (Key.Value.KeyChar) {
					case 'c':
						break;

					case 'q':
						Task.Run(async () => await Tess.Exit(0).ConfigureAwait(false));
						return false;

					default:
						Logger.Log("Unknown value entered! continuing to run the program...");
						break;
				}
			}

			Logger.Log("Generating default GPIO Config...");

			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config directory doesnt exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			if (File.Exists(Constants.GPIOConfigPath)) {
				return true;
			}

			TaskQueueRoot Config = new TaskQueueRoot {
				TaskList = new ConcurrentQueue<TaskStructure>()
			};

			JsonSerializer serializer = new JsonSerializer();
			JsonConvert.SerializeObject(Config, Formatting.Indented);
			string pathName = Constants.TaskQueueFilePath;
			using (StreamWriter sw = new StreamWriter(pathName, false))
			using (JsonWriter writer = new JsonTextWriter(sw)) {
				writer.Formatting = Formatting.Indented;
				serializer.Serialize(writer, Config);
				sw.Dispose();
			}
			return true;
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

					Task.Delay(100).Wait();
				}
			}, "Task queue listener");
		}
	}
}
