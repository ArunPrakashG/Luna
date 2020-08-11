using Luna.Logging;
using System;
using System.Collections.Generic;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Luna.Shell {
	internal class CommandInitializer {
		private static readonly InternalLogger Logger = new InternalLogger(nameof(CommandInitializer));
		private static readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);
		private static readonly SemaphoreSlim LoadSync = new SemaphoreSlim(1, 1);
		private HashSet<Assembly>? AssemblyCollection = new HashSet<Assembly>();

		internal async Task<bool> LoadInternalCommandsAsync<T>() where T : IShellCommand {
			Assembly currentAssembly = Assembly.GetExecutingAssembly();
			await LoadSync.WaitAsync().ConfigureAwait(false);

			try {
				ConventionBuilder conventions = new ConventionBuilder();
				conventions.ForTypesDerivedFrom<T>().Export<T>();
				IEnumerable<Assembly> psuedoCollection = new HashSet<Assembly>() {
					currentAssembly
				};

				ContainerConfiguration configuration = new ContainerConfiguration().WithAssemblies(psuedoCollection, conventions);
				using CompositionHost container = configuration.CreateContainer();
				List<T> list = container.GetExports<T>().ToList();

				if (list.Count <= 0) {
					return false;
				}

				foreach (T command in list) {
					if (await IsExistingCommand<T>(command.UniqueId).ConfigureAwait(false)) {
						Logger.Warn($"'{command.CommandName}' shell command is already loaded; skipping from loading process...");
						continue;
					}

					await command.InitAsync().ConfigureAwait(false);
					Interpreter.Commands.Add(command.CommandKey, command);
					Logger.Trace($"Loaded shell command -> {command.CommandName}");
				}

				return true;
			}
			catch (Exception e) {
				Logger.Exception(e);
				return false;
			}
			finally {
				LoadSync.Release();
			}
		}

		internal async Task<bool> LoadCommandsAsync<T>() where T : IShellCommand {
			if (!Directory.Exists(Constants.CommandsDirectory)) {
				Directory.CreateDirectory(Constants.CommandsDirectory);
			}

			AssemblyCollection?.Clear();
			AssemblyCollection = LoadAssemblies();

			if (AssemblyCollection == null || AssemblyCollection.Count <= 0) {
				Logger.Trace("No command assemblies found.");
				return false;
			}

			await LoadSync.WaitAsync().ConfigureAwait(false);

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
						Logger.Warn($"'{command.CommandName}' shell command already exists. skipping...");
						continue;
					}

					await command.InitAsync().ConfigureAwait(false);
					Interpreter.Commands.Add(command.CommandKey, command);
					Logger.Info($"Loaded external shell command -> {command.CommandName}");
				}

				return true;
			}
			catch (Exception e) {
				Logger.Exception(e);
				return false;
			}
			finally {
				LoadSync.Release();
			}
		}

		internal async Task<bool> UnloadCommandAsync<T>(string? cmdId) where T : IShellCommand {
			if (string.IsNullOrEmpty(cmdId)) {
				return false;
			}

			if (Interpreter.CommandsCount <= 0) {
				return false;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				foreach (KeyValuePair<string, IShellCommand> cmd in Interpreter.Commands) {
					if (string.IsNullOrEmpty(cmd.Key) || string.IsNullOrEmpty(cmd.Value.CommandKey)) {
						continue;
					}

					if (cmd.Value.UniqueId.Equals(cmdId)) {
						cmd.Value.Dispose();
						Interpreter.Commands.Remove(cmd.Value.UniqueId);
						Logger.Warn($"Shell command has been unloaded -> {cmd.Value.CommandName}");
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

		internal async Task<T> GetCommandWithIdAsync<T>(string? uniqueId) where T : IShellCommand {
			if (string.IsNullOrEmpty(uniqueId) || Interpreter.CommandsCount <= 0) {
				return default;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				foreach (KeyValuePair<string, IShellCommand> cmd in Interpreter.Commands) {
					if (string.IsNullOrEmpty(cmd.Key) || string.IsNullOrEmpty(cmd.Value.CommandKey)) {
						continue;
					}

					if (cmd.Value.UniqueId.Equals(uniqueId)) {
						return (T) cmd.Value;
					}
				}

				return default;
			}
			finally {
				Sync.Release();
			}
		}

		internal async Task<T> GetCommandWithKeyAsync<T>(string? commandKey) where T : IShellCommand {
			if (string.IsNullOrEmpty(commandKey) || Interpreter.CommandsCount <= 0) {
				return default;
			}

			await Sync.WaitAsync().ConfigureAwait(false);

			try {
				foreach (KeyValuePair<string, IShellCommand> commandPair in Interpreter.Commands) {
					if (string.IsNullOrEmpty(commandPair.Key) || string.IsNullOrEmpty(commandPair.Value.CommandKey)) {
						continue;
					}

					// if user entered only first letter of the command key and the command is unique to just 2, we match those two commands up.
					if (CommandStartsWithIsUnique(commandKey[0], commandPair.Value.CommandKey[0])) {
						return (T) commandPair.Value;
					}

					// else, compare both and return the command instance.
					if (commandPair.Value.CommandKey.Equals(commandKey, StringComparison.OrdinalIgnoreCase)) {
						return (T) commandPair.Value;
					}
				}

				return default;
			}
			finally {
				Sync.Release();
			}
		}

		private bool CommandStartsWithIsUnique(char commandKeyChar, char targetKeyChar) {
			if (commandKeyChar != targetKeyChar) {
				return false;
			}

			int commandKeyCharCount = 0;
			int targetKeyCharCount = 0;

			foreach (KeyValuePair<string, IShellCommand> command in Interpreter.Commands) {
				if (command.Value.CommandKey[0] == commandKeyChar) {
					commandKeyCharCount++;
				}

				if (command.Value.CommandKey[0] == targetKeyChar) {
					targetKeyCharCount++;
				}
			}

			return (commandKeyCharCount == 1) && (targetKeyCharCount == 1);
		}

		internal async Task<bool> SetOnExecuteFuncAsync<T>(string? id, Func<Parameter, bool> func) where T : IShellCommand {
			if (string.IsNullOrEmpty(id) || Interpreter.CommandsCount <= 0) {
				return false;
			}

			await Sync.WaitAsync().ConfigureAwait(false);
			try {
				foreach (var cmd in Interpreter.Commands) {
					if (string.IsNullOrEmpty(cmd.Key) || string.IsNullOrEmpty(cmd.Value.CommandKey)) {
						continue;
					}

					if (cmd.Value.CommandKey.Equals(id, StringComparison.OrdinalIgnoreCase)) {
						cmd.Value.OnExecuteFunc = func;
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

			if (Interpreter.CommandsCount <= 0) {
				return false;
			}

			await Sync.WaitAsync().ConfigureAwait(false);
			try {
				foreach (KeyValuePair<string, IShellCommand> cmd in Interpreter.Commands) {
					if (string.IsNullOrEmpty(cmd.Key) || string.IsNullOrEmpty(cmd.Value.CommandKey)) {
						continue;
					}

					if (cmd.Value.UniqueId.Equals(id)) {
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

			if (string.IsNullOrEmpty(Constants.HomeDirectory) || string.IsNullOrEmpty(Constants.CommandsDirectory)) {
				return null;
			}

			string pluginsPath = Path.Combine(Constants.HomeDirectory, Constants.CommandsDirectory);

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
				Logger.NullError(nameof(path));
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
						Logger.Warn($"Assembly path is invalid. {assemblyPath}");
						Logger.Exception(e);
						continue;
					}

					assemblies.Add(assembly);
				}
			}
			catch (Exception e) {
				Logger.Exception(e);
				return null;
			}

			return assemblies;
		}
	}
}
