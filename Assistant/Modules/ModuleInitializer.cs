
//    _  _  ___  __  __ ___     _   ___ ___ ___ ___ _____ _   _  _ _____
//   | || |/ _ \|  \/  | __|   /_\ / __/ __|_ _/ __|_   _/_\ | \| |_   _|
//   | __ | (_) | |\/| | _|   / _ \\__ \__ \| |\__ \ | |/ _ \| .` | | |
//   |_||_|\___/|_|  |_|___| /_/ \_\___/___/___|___/ |_/_/ \_\_|\_| |_|
//

//MIT License

//Copyright(c) 2019 Arun Prakash
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;
using Assistant.Modules.Interfaces;
using Assistant.Modules.Interfaces.EventInterfaces;
using System;
using System.Collections.Generic;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Assistant.AssistantCore.Enums;

namespace Assistant.Modules {
	public class ModuleInfo<T> where T : IModuleBase {
		public string ModuleIdentifier { get; set; }
		public ModuleType ModuleType { get; set; }
		public T Module { get; set; }
		public bool IsLoaded { get; set; }
	}

	public class ModuleInitializer {
		private readonly Logger Logger = new Logger("MODULES");
		public HashSet<Assembly> AssemblyCollection { get; set; } = new HashSet<Assembly>();
		private static readonly SemaphoreSlim ModuleLoaderSemaphore = new SemaphoreSlim(1, 1);
		public List<IModuleBase> Modules { get; set; } = new List<IModuleBase>();

		private List<ModuleInfo<IModuleBase>> ModulesCache = new List<ModuleInfo<IModuleBase>>();

		public async Task<bool> LoadAsync() {
			if (!Core.Config.EnableModules) {
				return false;
			}

			AssemblyCollection.Clear();
			AssemblyCollection = LoadAssemblies();

			if (AssemblyCollection == null || AssemblyCollection.Count <= 0) {
				Logger.Log("No assemblies found.", LogLevels.Trace);
				return false;
			}

			await ModuleLoaderSemaphore.WaitAsync().ConfigureAwait(false);

			try {
				ConventionBuilder conventions = new ConventionBuilder();
				conventions.ForTypesDerivedFrom<IModuleBase>().Export<IModuleBase>();
				ContainerConfiguration configuration = new ContainerConfiguration().WithAssemblies(AssemblyCollection, conventions);

				using (CompositionHost container = configuration.CreateContainer()) {
					List<IModuleBase> list = container.GetExports<IModuleBase>().ToList();

					if (list.Count > 0) {
						foreach (IModuleBase bot in list) {
							Logger.Log($"Loading module of type {ParseModuleType(bot.ModuleType)} ...", LogLevels.Trace);

							bot.ModuleIdentifier = GenerateModuleIdentifier();
							ModuleInfo<IModuleBase> data = new ModuleInfo<IModuleBase>() {
								IsLoaded = true,
								Module = bot,
								ModuleIdentifier = bot.ModuleIdentifier,
								ModuleType = ParseModuleType(bot.ModuleType)
							};
							
							Logger.Log($"Successfully loaded module with id {bot.ModuleIdentifier} of type {data.ModuleType.ToString()}", LogLevels.Trace);

							if(Modules.Count > 0 && !Modules.Contains(bot)) {
								Modules.Add(bot);
							}
							
							ModulesCache.Add(data);
						}
					}
				}
			}
			finally {
				ModuleLoaderSemaphore.Release();
			}

			return true;
		}

		public async Task InitServiceAsync() {
			if (Modules.Count <= 0) {
				return;
			}

			foreach (IModuleBase module in Modules) {
				await InitServiceOfTypeAsync<IModuleBase>(module).ConfigureAwait(false);
			}
		}

		private async Task<bool> InitServiceOfTypeAsync<TType>(TType module) where TType : IModuleBase {
			if (module == null) {
				return false;
			}

			Logger.Log($"Starting {ParseModuleType(module.ModuleType).ToString()} module... ({module.ModuleIdentifier})", LogLevels.Trace);

			if (IsExisitingModule(module)) {
				return false;
			}

			if (module.RequiresInternetConnection && !Core.IsNetworkAvailable) {
				return false;
			}

			await ModuleLoaderSemaphore.WaitAsync().ConfigureAwait(false);
			if (module.InitModuleService()) {
				Logger.Log($"Module loaded! {module.ModuleIdentifier}/{ParseModuleType(module.ModuleType).ToString()}", LogLevels.Info);
				ModuleLoaderSemaphore.Release();
				return true;
			}

			ModuleLoaderSemaphore.Release();
			return false;
		}

		private static ModuleType ParseModuleType(int typeCode) {
			if (typeCode < 0 || typeCode > 4) {
				return ModuleType.Unknown;
			}

			return (ModuleType) typeCode;
		}

		public bool UnloadModulesOfType<TType>() where TType : IModuleBase {
			if (Modules.Count <= 0) {
				return false;
			}

			if (!Modules.OfType<TType>().Any()) {
				return false;
			}

			List<TType> unloadedModules = new List<TType>();
			foreach (TType mod in Modules.OfType<TType>()) {
				if (mod.IsLoaded && mod.InitModuleShutdown()) {
					Logger.Log($"Module of type {ParseModuleType(mod.ModuleType)} has been unloaded.", LogLevels.Trace);
					mod.IsLoaded = false;
					unloadedModules.Add(mod);
					continue;
				}
			}

			if (unloadedModules.Count > 0) {
				foreach (TType mod in unloadedModules) {
					if (Modules[Modules.IndexOf(mod)] != null) {
						Modules.RemoveAt(Modules.IndexOf(mod));
						Logger.Log($"Module of type {ParseModuleType(mod.ModuleType)} has been removed from collection.", LogLevels.Trace);
					}
				}
			}

			return true;
		}

		public bool UnloadModuleWithId(string Id) {
			if (Helpers.IsNullOrEmpty(Id) || Modules.Count <= 0) {
				return false;
			}

			IModuleBase module = Modules.Find(x => x.IsLoaded && x.ModuleIdentifier == Id);
			return module.InitModuleShutdown();
		}

		public ModuleInfo<TType> FindModuleOfType<TType>(string identifier) where TType : IModuleBase {
			if (Helpers.IsNullOrEmpty(identifier) || ModulesCache.Count <= 0 || !ModulesCache.OfType<TType>().Any()) {
				return null;
			}

			foreach (ModuleInfo<IModuleBase> mod in ModulesCache) {
				if (mod.ModuleIdentifier == identifier) {
					Logger.Log($"Module found of type {mod.ModuleType} with identifier {mod.ModuleIdentifier}", LogLevels.Trace);

					ModuleInfo<TType> module = new ModuleInfo<TType>() {
						IsLoaded = mod.IsLoaded,
						ModuleType = mod.ModuleType,
						ModuleIdentifier = mod.ModuleIdentifier,
						Module = (TType) mod.Module
					};

					return module;
				}
			}

			return null;
		}

		private bool IsExisitingModule<TType>(TType module) where TType : IModuleBase {
			if (module == null) {
				return false;
			}

			if (Modules.Count <= 0) {
				return false;
			}

			if (Modules.Any(x => x.ModuleIdentifier == module.ModuleIdentifier)) {
				Logger.Log("This module is already loaded and added to module collection.", LogLevels.Trace);
				return true;
			}

			return false;
		}

		public bool UnloadFromPath(string assemblyPath) {
			if (Helpers.IsNullOrEmpty(assemblyPath)) {
				return false;
			}

			if (Modules.Count <= 0) {
				return false;
			}

			assemblyPath = Path.GetFullPath(assemblyPath);
			IModuleBase module = Modules.Find(x => x.IsLoaded && x.ModulePath == assemblyPath);

			if (module == null) {
				return false;
			}

			return module.InitModuleShutdown();
		}

		private HashSet<Assembly> LoadAssemblies() {
			HashSet<Assembly> assemblies = null;

			string pluginsPath = Path.Combine(Constants.HomeDirectory, Constants.ModuleDirectory);

			if (Directory.Exists(pluginsPath)) {
				HashSet<Assembly> loadedAssemblies = LoadAssembliesFromPath(pluginsPath);

				if ((loadedAssemblies != null) && (loadedAssemblies.Count > 0)) {
					assemblies = loadedAssemblies;
				}
			}

			return assemblies;
		}

		private HashSet<Assembly> LoadAssembliesFromPath(string path) {
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
						Logger.Log($"Assembly path is invalid. {assemblyPath}", Enums.LogLevels.Warn);
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

		public void OnCoreShutdown() => UnloadModulesOfType<IModuleBase>();

		public async Task<bool> ExecuteAsyncEvent(Enums.AsyncModuleContext context, object fileEventSender = null, FileSystemEventArgs fileEventArgs = null) {
			if (Modules.Count <= 0 || !Modules.OfType<IAsyncEventBase>().Any()) {
				return false;
			}

			foreach (IAsyncEventBase eventMod in Modules.OfType<IAsyncEventBase>()) {
				switch (context) {
					case Enums.AsyncModuleContext.AssistantShutdown: {
							if (!eventMod.IsLoaded) {
								continue;
							}

							return await eventMod.OnAssistantShutdownRequestedAsync().ConfigureAwait(false);
						}

					case Enums.AsyncModuleContext.AssistantStartup: {
							return await eventMod.OnAssistantStartedAsync().ConfigureAwait(false);
						}

					case Enums.AsyncModuleContext.ConfigWatcherEvent: {
							if (!eventMod.IsLoaded) {
								continue;
							}

							if (fileEventArgs == null || fileEventSender == null) {
								return false;
							}

							return await eventMod.OnConfigWatcherEventRasiedAsync(fileEventSender, fileEventArgs).ConfigureAwait(false);
						}

					case Enums.AsyncModuleContext.ModuleWatcherEvent: {
							if (!eventMod.IsLoaded) {
								continue;
							}

							if (fileEventArgs == null || fileEventSender == null) {
								return false;
							}

							return await eventMod.OnModuleWatcherEventRasiedAsync(fileEventSender, fileEventArgs).ConfigureAwait(false);
						}

					case Enums.AsyncModuleContext.NetworkDisconnected: {
							if (!eventMod.IsLoaded) {
								continue;
							}

							return await eventMod.OnNetworkDisconnectedAsync().ConfigureAwait(false);
						}

					case Enums.AsyncModuleContext.NetworkReconnected: {
							if (!eventMod.IsLoaded) {
								continue;
							}

							return await eventMod.OnNetworkReconnectedAsync().ConfigureAwait(false);
						}

					case Enums.AsyncModuleContext.SystemRestart: {
							if (!eventMod.IsLoaded) {
								continue;
							}

							return await eventMod.OnSystemRestartRequestedAsync().ConfigureAwait(false);
						}

					case Enums.AsyncModuleContext.SystemShutdown: {
							if (!eventMod.IsLoaded) {
								continue;
							}

							return await eventMod.OnSystemShutdownRequestedAsync().ConfigureAwait(false);
						}

					case Enums.AsyncModuleContext.UpdateAvailable: {
							if (!eventMod.IsLoaded) {
								continue;
							}

							return await eventMod.OnUpdateAvailableAsync().ConfigureAwait(false);
						}

					case Enums.AsyncModuleContext.UpdateStarted: {
							if (!eventMod.IsLoaded) {
								continue;
							}

							return await eventMod.OnUpdateStartedAsync().ConfigureAwait(false);
						}

					default: {
							return false;
						}
				}
			}

			return false;
		}

		private string GenerateModuleIdentifier() {
			StringBuilder builder = new StringBuilder();
			Enumerable
				.Range(65, 26)
				.Select(e => ((char) e).ToString())
				.Concat(Enumerable.Range(97, 26).Select(e => ((char) e).ToString()))
				.Concat(Enumerable.Range(0, 10).Select(e => e.ToString()))
				.OrderBy(e => Guid.NewGuid())
				.Take(11)
				.ToList().ForEach(e => builder.Append(e));
			return builder.ToString();
		}
	}
}
