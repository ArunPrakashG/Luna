using Assistant.Extensions;
using Assistant.Extensions.Shared.Shell;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Core.Shell.Commands {
	internal class Initializer {
		private static readonly ILogger Logger = new Logger(typeof(Initializer).Name);
		private static readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);
		private HashSet<Assembly>? AssemblyCollection = new HashSet<Assembly>();
		internal static readonly ObservableCollection<IShellCommand> Commands = new ObservableCollection<IShellCommand>();

		static Initializer() {
			Commands.CollectionChanged += OnCommandCollectionChanged;
		}

		private static void OnCommandCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			Logger.Trace($"Commands collection changed -> {e.Action.ToString()}");
		}

		internal async Task<bool> LoadCommandsAsync<T>() where T : IShellCommand {
			if (!Directory.Exists(Constants.COMMANDS_PATH)) {
				Directory.CreateDirectory(Constants.COMMANDS_PATH);
			}

			AssemblyCollection?.Clear();
			AssemblyCollection = LoadAssemblies();

			if (AssemblyCollection == null || AssemblyCollection.Count <= 0) {
				Logger.Trace("No command assemblies found.");
				return false;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				ConventionBuilder conventions = new ConventionBuilder();
				conventions.ForTypesDerivedFrom<T>().Export<T>();
				ContainerConfiguration configuration = new ContainerConfiguration().WithAssemblies(AssemblyCollection, conventions);

				using CompositionHost container = configuration.CreateContainer();
				List<T> list = container.GetExports<T>().ToList();

				if (list.Count <= 0) {
					return false;
				}

				foreach (T command in list) {
					if (await IsExistingCommand<T>(command.UniqueId).ConfigureAwait(false)) {
						Logger.Warning($"{command.CommandName} shell command already exists. skipping...");
						continue;
					}

					await command.InitAsync().ConfigureAwait(false);
					Commands.Add(command);
					Logger.Info($"Loaded shell command -> {command.CommandName}");
				}

				return true;
			}
			catch (Exception e) {
				Logger.Exception(e);
				return false;
			}
			finally {
				Sync.Release();
			}
		}

		internal async Task<bool> UnloadCommandAsync<T>(string? cmdId) where T : IShellCommand {
			if (string.IsNullOrEmpty(cmdId)) {
				return false;
			}

			if (Commands.Count <= 0) {
				return false;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				foreach (T cmd in Commands) {
					if (cmd == null || string.IsNullOrEmpty(cmd.UniqueId)) {
						continue;
					}

					if (cmd.UniqueId.Equals(cmdId)) {
						cmd.Dispose();
						Commands.Remove(cmd);
						Logger.Warning($"Shell command has been unloaded -> {cmd.CommandName}");
						return true;
					}
				}

				return false;
			}
			catch (Exception e) {
				Logger.Exception(e);
				return false;
			}
			finally {
				Sync.Release();
			}
		}

		internal async Task<T> GetCommandWithIdAsync<T>(string? id) where T : IShellCommand {
			if (string.IsNullOrEmpty(id) || Commands.Count <= 0) {
				return default;
			}

			await Sync.WaitAsync().ConfigureAwait(false);
			try {
				foreach (T cmd in Commands) {
					if (cmd == null || string.IsNullOrEmpty(cmd.UniqueId)) {
						continue;
					}

					if (cmd.UniqueId.Equals(id)) {
						return cmd;
					}
				}

				return default;
			}
			finally {
				Sync.Release();
			}
		}

		internal async Task<T> GetCommandWithKeyAsync<T>(string? commandKey) where T : IShellCommand {
			if (string.IsNullOrEmpty(commandKey) || Commands.Count <= 0) {
				return default;
			}

			await Sync.WaitAsync().ConfigureAwait(false);
			try {
				foreach (T cmd in Commands) {
					if (cmd == null || string.IsNullOrEmpty(cmd.UniqueId)) {
						continue;
					}

					if (cmd.CommandKey.Equals(commandKey, StringComparison.OrdinalIgnoreCase)) {
						return cmd;
					}
				}

				return default;
			}
			finally {
				Sync.Release();
			}
		}

		internal async Task<bool> SetOnExecuteFuncAsync<T>(string? id, Func<Parameter, bool> func) where T : IShellCommand {
			if (string.IsNullOrEmpty(id) || Commands.Count <= 0) {
				return false;
			}

			await Sync.WaitAsync().ConfigureAwait(false);
			try {
				foreach (T cmd in Commands) {
					if (cmd == null || string.IsNullOrEmpty(cmd.UniqueId)) {
						continue;
					}

					if (cmd.CommandKey.Equals(id, StringComparison.OrdinalIgnoreCase)) {
						cmd.OnExecuteFunc = func;
						return true;
					}
				}

				return false;
			}
			finally {
				Sync.Release();
			}
		}

		private async Task<bool> IsExistingCommand<T>(string? id) where T : IShellCommand {
			if (string.IsNullOrEmpty(id)) {
				return true;
			}

			if (Commands.Count <= 0) {
				return false;
			}

			await Sync.WaitAsync().ConfigureAwait(false);
			try {
				foreach (T cmd in Commands) {
					if (cmd == null || string.IsNullOrEmpty(cmd.UniqueId)) {
						continue;
					}

					if (cmd.UniqueId.Equals(id)) {
						return true;
					}
				}
			}
			finally {
				Sync.Release();
			}

			return false;
		}

		private HashSet<Assembly>? LoadAssemblies() {
			HashSet<Assembly> assemblies = new HashSet<Assembly>();

			if (string.IsNullOrEmpty(Constants.HomeDirectory) || string.IsNullOrEmpty(Constants.COMMANDS_PATH)) {
				return null;
			}

			string pluginsPath = Path.Combine(Constants.HomeDirectory, Constants.COMMANDS_PATH);

			if (Directory.Exists(pluginsPath)) {
				HashSet<Assembly>? loadedAssemblies = LoadAssembliesFromPath(pluginsPath);

				if ((loadedAssemblies != null) && (loadedAssemblies.Count > 0)) {
					assemblies = loadedAssemblies;
				}
			}

			return assemblies;
		}

		private HashSet<Assembly>? LoadAssembliesFromPath(string path) {
			if (string.IsNullOrEmpty(path)) {
				Logger.Log(nameof(path));
				return null;
			}

			if (!Directory.Exists(path)) {
				return null;
			}

			HashSet<Assembly> assemblies = new HashSet<Assembly>();

			try {
				foreach (string assemblyPath in Directory.EnumerateFiles(path, "*.dll", SearchOption.AllDirectories)) {
					Assembly assembly;

					try {
						assembly = Assembly.LoadFrom(assemblyPath);
					}
					catch (Exception e) {
						Logger.Warning($"Assembly path is invalid. {assemblyPath}");
						Logger.Log(e);
						continue;
					}

					assemblies.Add(assembly);
				}
			}
			catch (Exception e) {
				Logger.Log(e);
				return null;
			}

			return assemblies;
		}
	}
}
