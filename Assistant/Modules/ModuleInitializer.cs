
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

	public class ModulesCollection {
		public List<ModuleInfo<IEmailClient>> EmailClients { get; set; } = new List<ModuleInfo<IEmailClient>>();
		public List<ModuleInfo<IDiscordBot>> DiscordBots { get; set; } = new List<ModuleInfo<IDiscordBot>>();
		public List<ModuleInfo<ISteamClient>> SteamClients { get; set; } = new List<ModuleInfo<ISteamClient>>();
		public List<ModuleInfo<IAsyncEventBase>> AsyncEventModules { get; set; } = new List<ModuleInfo<IAsyncEventBase>>();
		public List<ModuleInfo<IYoutubeClient>> YoutubeClients { get; set; } = new List<ModuleInfo<IYoutubeClient>>();

		//TODO: Merge multiple to single list
		public List<ModuleInfo<IModuleBase>> Collection { get; set; } = new List<ModuleInfo<IModuleBase>>();

		public bool IsModulesEmpty =>
			EmailClients.Count() <= 0 && DiscordBots.Count() <= 0 &&
			SteamClients.Count() <= 0 && YoutubeClients.Count() <= 0 && AsyncEventModules.Count() <= 0;
	}

	public class ModuleInitializer {
		private readonly Logger Logger = new Logger("MODULES");
		public ModulesCollection ModulesCollection { get; private set; } = new ModulesCollection();
		public HashSet<Assembly> AssemblyCollection { get; set; } = new HashSet<Assembly>();
		private static readonly SemaphoreSlim ModuleLoaderSemaphore = new SemaphoreSlim(1, 1);

		public bool UnloadModulesOfType<T>(T module) where T : IModuleBase {
			if (ModulesCollection == null || ModulesCollection.IsModulesEmpty) {
				return false;
			}

			return Unload(module);
		}

		public bool UnloadModulesOfType(string identifier) {
			if (ModulesCollection == null || ModulesCollection.IsModulesEmpty || Helpers.IsNullOrEmpty(identifier)) {
				return false;
			}

			return Unload(identifier);
		}

		public bool UnloadModulesOfType<T>(List<ModuleInfo<T>> moduleCollection) where T : IModuleBase {
			if (ModulesCollection == null || ModulesCollection.IsModulesEmpty || moduleCollection.Count() <= 0) {
				return false;
			}

			int unloadedCount = 0;
			foreach (ModuleInfo<T> module in moduleCollection.ToList()) {
				unloadedCount = module.IsLoaded && UnloadModulesOfType<T>(module.Module) ? unloadedCount++ : unloadedCount;
			}

			bool result = unloadedCount == moduleCollection.Count() ? true : false;
			return result;
		}

		private ModuleInfo<IDiscordBot> FindDiscordModule(string identifier) {
			if (!Helpers.IsNullOrEmpty(identifier) && !ModulesCollection.IsModulesEmpty && ModulesCollection.DiscordBots.Count() > 0) {
				foreach (ModuleInfo<IDiscordBot> mod in ModulesCollection.DiscordBots) {
					if (mod.ModuleIdentifier == identifier) {
						Logger.Log($"Module found with identifier {identifier} [DISCORD]", LogLevels.Trace);
						return mod;
					}
				}
			}

			return null;
		}

		private ModuleInfo<ISteamClient> FindSteamModule(string identifier) {
			if (!Helpers.IsNullOrEmpty(identifier) && !ModulesCollection.IsModulesEmpty && ModulesCollection.SteamClients.Count() > 0) {
				foreach (ModuleInfo<ISteamClient> mod in ModulesCollection.SteamClients) {
					if (mod.ModuleIdentifier == identifier) {
						Logger.Log($"Module found with identifier {identifier} [STEAM]", LogLevels.Trace);
						return mod;
					}
				}
			}

			return null;
		}

		private ModuleInfo<IAsyncEventBase> FindEventModule(string identifier) {
			if (!Helpers.IsNullOrEmpty(identifier) && !ModulesCollection.IsModulesEmpty && ModulesCollection.AsyncEventModules.Count() > 0) {
				foreach (ModuleInfo<IAsyncEventBase> mod in ModulesCollection.AsyncEventModules) {
					if (mod.ModuleIdentifier == identifier) {
						Logger.Log($"Module found with identifier {identifier} [EVENT]", LogLevels.Trace);
						return mod;
					}
				}
			}

			return null;
		}

		private ModuleInfo<IYoutubeClient> FindYoutubeModule(string identifier) {
			if (!Helpers.IsNullOrEmpty(identifier) && !ModulesCollection.IsModulesEmpty && ModulesCollection.YoutubeClients.Count() > 0) {
				foreach (ModuleInfo<IYoutubeClient> mod in ModulesCollection.YoutubeClients) {
					if (mod.ModuleIdentifier == identifier) {
						Logger.Log($"Module found with identifier {identifier} [YOUTUBE]", LogLevels.Trace);
						return mod;
					}
				}
			}

			return null;
		}

		private ModuleInfo<IEmailClient> FindEmailModule(string identifier) {
			if (!Helpers.IsNullOrEmpty(identifier) && !ModulesCollection.IsModulesEmpty && ModulesCollection.EmailClients.Count() > 0) {
				foreach (ModuleInfo<IEmailClient> mod in ModulesCollection.EmailClients) {
					if (mod.ModuleIdentifier == identifier) {
						Logger.Log($"Module found with identifier {identifier} [EMAIL]", LogLevels.Trace);
						return mod;
					}
				}
			}

			return null;
		}

		private bool IsExisitingModule<T>(T module) where T : IModuleBase {
			if (module == null) {
				return false;
			}

			if (ModulesCollection.IsModulesEmpty) {
				return false;
			}

			if (ModulesCollection.DiscordBots.Count > 0) {
				foreach (ModuleInfo<IDiscordBot> mod in ModulesCollection.DiscordBots) {
					if (mod.ModuleIdentifier == module.ModuleIdentifier) {
						Logger.Log("This module is already loaded and added to module collection. [DISCORD]", LogLevels.Trace);
						return true;
					}
				}
			}

			if (ModulesCollection.EmailClients.Count > 0) {
				foreach (ModuleInfo<IEmailClient> mod in ModulesCollection.EmailClients) {
					if (mod.ModuleIdentifier == module.ModuleIdentifier) {
						Logger.Log("This module is already loaded and added to module collection. [EMAIL]", LogLevels.Trace);
						return true;
					}
				}
			}

			if (ModulesCollection.SteamClients.Count > 0) {
				foreach (ModuleInfo<ISteamClient> mod in ModulesCollection.SteamClients) {
					if (mod.ModuleIdentifier == module.ModuleIdentifier) {
						Logger.Log("This module is already loaded and added to module collection. [STEAM]", LogLevels.Trace);
						return true;
					}
				}
			}

			if (ModulesCollection.AsyncEventModules.Count > 0) {
				foreach (ModuleInfo<IAsyncEventBase> mod in ModulesCollection.AsyncEventModules) {
					if (mod.ModuleIdentifier == module.ModuleIdentifier) {
						Logger.Log("This module is already loaded and added to module collection. [EVENT]", LogLevels.Trace);
						return true;
					}
				}
			}

			if (ModulesCollection.YoutubeClients.Count > 0) {
				foreach (ModuleInfo<IYoutubeClient> mod in ModulesCollection.YoutubeClients) {
					if (mod.ModuleIdentifier == module.ModuleIdentifier) {
						Logger.Log("This module is already loaded and added to module collection. [YOUTUBE]", LogLevels.Trace);
						return true;
					}
				}
			}

			return false;
		}

		public bool LoadAndStartModulesOfType<T>(bool moduleWatcherInvoke = false) where T : IModuleBase {
			if (!moduleWatcherInvoke && !ModulesCollection.IsModulesEmpty) {
				return false;
			}

			AssemblyCollection = LoadAssemblies();

			if (AssemblyCollection == null || AssemblyCollection.Count <= 0) {
				Logger.Log("No assemblies found.", LogLevels.Trace);
				return false;
			}

			return Load<T>();
		}

		private bool InitLoadedModuleService<T>(IEnumerable<T> loadedCollection) where T : IModuleBase {
			if (loadedCollection == null || loadedCollection.Count() == 0) {
				return false;
			}

			int loadedModuleCount = 0;
			ModuleLoaderSemaphore.Wait();

			foreach (T value in loadedCollection) {
				if (value == null) {
					continue;
				}

				Logger.Log($"Starting {((ModuleType) value.ModuleType).ToString()} module... ({value.ModuleIdentifier})", LogLevels.Trace);

				if (IsExisitingModule(value)) {
					continue;
				}

				if (value.RequiresInternetConnection && !Core.IsNetworkAvailable) {
					continue;
				}

				if (value.InitModuleService()) {
					Logger.Log($"Starting successfull! {value.ModuleIdentifier}/{((ModuleType) value.ModuleType).ToString()}", LogLevels.Info);
					switch (value.ModuleType) {
						case 0:
							ModulesCollection.DiscordBots.Add(new ModuleInfo<IDiscordBot>() {
								IsLoaded = true,
								Module = (IDiscordBot) value,
								ModuleIdentifier = value.ModuleIdentifier,
								ModuleType = (ModuleType) value.ModuleType
							});
							break;
						case 1:
							ModulesCollection.EmailClients.Add(new ModuleInfo<IEmailClient>() {
								IsLoaded = true,
								Module = (IEmailClient) value,
								ModuleIdentifier = value.ModuleIdentifier,
								ModuleType = (ModuleType) value.ModuleType
							});
							break;
						case 2:
							ModulesCollection.SteamClients.Add(new ModuleInfo<ISteamClient>() {
								IsLoaded = true,
								Module = (ISteamClient) value,
								ModuleIdentifier = value.ModuleIdentifier,
								ModuleType = (ModuleType) value.ModuleType
							});
							break;
						case 3:
							ModulesCollection.YoutubeClients.Add(new ModuleInfo<IYoutubeClient>() {
								IsLoaded = true,
								Module = (IYoutubeClient) value,
								ModuleIdentifier = value.ModuleIdentifier,
								ModuleType = (ModuleType) value.ModuleType
							});
							break;
						case 4:
							ModulesCollection.AsyncEventModules.Add(new ModuleInfo<IAsyncEventBase>() {
								IsLoaded = true,
								Module = (IAsyncEventBase) value,
								ModuleIdentifier = value.ModuleIdentifier,
								ModuleType = (ModuleType) value.ModuleType
							});
							break;
					}

					loadedModuleCount++;
				}
			}

			Logger.Log("Finished starting all modules!", LogLevels.Trace);
			ModuleLoaderSemaphore.Release();
			return true;
		}

		private bool Load<T>() where T : IModuleBase {
			if (!Core.Config.EnableModules) {
				return false;
			}

			ConventionBuilder conventions = new ConventionBuilder();
			conventions.ForTypesDerivedFrom<T>().Export<T>();
			ContainerConfiguration configuration = new ContainerConfiguration().WithAssemblies(AssemblyCollection, conventions);

			(bool status, List<X> loadedModules) loadOfType<X>() where X : IModuleBase {
				List<X> modules = new List<X>();

				using (CompositionHost container = configuration.CreateContainer()) {
					List<X> list = container.GetExports<X>().ToList();

					if (list.Count > 0) {
						foreach (X bot in list) {
							Logger.Log($"Loading module of type {(ModuleType) bot.ModuleType} ...", LogLevels.Trace);
							bot.ModuleIdentifier = GenerateModuleIdentifier();
							modules.Add(bot);
							Logger.Log($"Successfully loaded module with id {bot.ModuleIdentifier} of type {bot.ModuleType.ToString()}", LogLevels.Trace);
						}

						return (true, modules);
					}
				}

				if (modules.Count <= 0) {
					return (false, null);
				}

				return (false, null);
			}

			(bool status, List<T> loadedModules) = loadOfType<T>();

			return status ? InitLoadedModuleService<T>(loadedModules) : false;
		}

		private bool UnloadAndRemove(string identifier) {
			if (Helpers.IsNullOrEmpty(identifier)) {
				return false;
			}

			ModuleInfo<IDiscordBot> discord = FindDiscordModule(identifier);
			if (discord != null) {
				return discord.IsLoaded && discord.Module.InitModuleShutdown() && ModulesCollection.DiscordBots.Remove(discord) ? true : false;
			}

			ModuleInfo<IEmailClient> email = FindEmailModule(identifier);
			if (email != null) {
				return email.IsLoaded && email.Module.InitModuleShutdown() && ModulesCollection.EmailClients.Remove(email) ? true : false;
			}

			ModuleInfo<IYoutubeClient> youtube = FindYoutubeModule(identifier);
			if (youtube != null) {
				return youtube.IsLoaded && youtube.Module.InitModuleShutdown() && ModulesCollection.YoutubeClients.Remove(youtube) ? true : false;
			}

			ModuleInfo<ISteamClient> steam = FindSteamModule(identifier);
			if (steam != null) {
				return steam.IsLoaded && steam.Module.InitModuleShutdown() && ModulesCollection.SteamClients.Remove(steam) ? true : false;
			}

			ModuleInfo<IAsyncEventBase> eventBase = FindEventModule(identifier);
			if (eventBase != null) {
				return eventBase.IsLoaded && eventBase.Module.InitModuleShutdown() && ModulesCollection.AsyncEventModules.Remove(eventBase) ? true : false;
			}

			return false;
		}

		private bool Remove(string identifier) {
			if (Helpers.IsNullOrEmpty(identifier)) {
				return false;
			}

			ModuleInfo<IDiscordBot> discord = FindDiscordModule(identifier);
			if (discord != null) {
				return ModulesCollection.DiscordBots.Remove(discord);
			}

			ModuleInfo<IEmailClient> email = FindEmailModule(identifier);
			if (email != null) {
				return ModulesCollection.EmailClients.Remove(email);
			}

			ModuleInfo<IYoutubeClient> youtube = FindYoutubeModule(identifier);
			if (youtube != null) {
				return ModulesCollection.YoutubeClients.Remove(youtube);
			}

			ModuleInfo<ISteamClient> steam = FindSteamModule(identifier);
			if (steam != null) {
				return ModulesCollection.SteamClients.Remove(steam);
			}

			ModuleInfo<IAsyncEventBase> eventBase = FindEventModule(identifier);
			if (eventBase != null) {
				return ModulesCollection.AsyncEventModules.Remove(eventBase);
			}

			return false;
		}

		private bool Unload<T>(T module) where T : IModuleBase {
			if (module == null || ModulesCollection.IsModulesEmpty) {
				return false;
			}

			if (UnloadAndRemove(module.ModuleIdentifier)) {
				Logger.Log($"Module of type {(ModuleType) module.ModuleType} has been unloaded.", LogLevels.Trace);
				return true;
			}

			return false;
		}

		private bool Unload(string identifier) {
			if (Helpers.IsNullOrEmpty(identifier) || ModulesCollection.IsModulesEmpty) {
				return false;
			}

			if (UnloadAndRemove(identifier)) {
				Logger.Log($"Module with {identifier} identifier has been unloaded.", LogLevels.Trace);
				return true;
			}

			return false;
		}

		public bool UnloadFromPath(string assemblyPath) {
			if (Helpers.IsNullOrEmpty(assemblyPath)) {
				return false;
			}

			if (ModulesCollection.IsModulesEmpty) {
				return false;
			}

			assemblyPath = Path.GetFullPath(assemblyPath);

			if (ModulesCollection.EmailClients.Count > 0) {
				foreach (ModuleInfo<IEmailClient> client in ModulesCollection.EmailClients) {
					if (client.Module.ModulePath == assemblyPath) {
						return Unload(client.ModuleIdentifier);
					}
				}
			}

			if (ModulesCollection.SteamClients.Count > 0) {
				foreach (ModuleInfo<ISteamClient> client in ModulesCollection.SteamClients) {
					if (client.Module.ModulePath == assemblyPath) {
						return Unload(client.ModuleIdentifier);
					}
				}
			}

			if (ModulesCollection.YoutubeClients.Count > 0) {
				foreach (ModuleInfo<IYoutubeClient> client in ModulesCollection.YoutubeClients) {
					if (client.Module.ModulePath == assemblyPath) {
						return Unload(client.ModuleIdentifier);
					}
				}
			}

			if (ModulesCollection.DiscordBots.Count > 0) {
				foreach (ModuleInfo<IDiscordBot> client in ModulesCollection.DiscordBots) {
					if (client.Module.ModulePath == assemblyPath) {
						return Unload(client.ModuleIdentifier);
					}
				}
			}

			if (ModulesCollection.AsyncEventModules.Count > 0) {
				foreach (ModuleInfo<IAsyncEventBase> client in ModulesCollection.AsyncEventModules) {
					if (client.Module.ModulePath == assemblyPath) {
						return Unload(client.ModuleIdentifier);
					}
				}
			}

			return false;
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

		public void OnCoreShutdown() {
			UnloadModulesOfType<IDiscordBot>(ModulesCollection.DiscordBots);
			UnloadModulesOfType<ISteamClient>(ModulesCollection.SteamClients);
			UnloadModulesOfType<IYoutubeClient>(ModulesCollection.YoutubeClients);
			UnloadModulesOfType<IAsyncEventBase>(ModulesCollection.AsyncEventModules);
			UnloadModulesOfType<IEmailClient>(ModulesCollection.EmailClients);
		}

		public async Task<bool> ExecuteAsyncEvent(Enums.AsyncModuleContext context, object fileEventSender = null, FileSystemEventArgs fileEventArgs = null) {
			if (ModulesCollection.IsModulesEmpty || ModulesCollection.AsyncEventModules.Count <= 0) {
				return false;
			}

			foreach (ModuleInfo<IAsyncEventBase> eventMod in ModulesCollection.AsyncEventModules) {
				switch (context) {
					case Enums.AsyncModuleContext.AssistantShutdown: {
							if (!eventMod.IsLoaded) {
								continue;
							}

							return await eventMod.Module.OnAssistantShutdownRequestedAsync().ConfigureAwait(false);
						}

					case Enums.AsyncModuleContext.AssistantStartup: {
							return await eventMod.Module.OnAssistantStartedAsync().ConfigureAwait(false);
						}

					case Enums.AsyncModuleContext.ConfigWatcherEvent: {
							if (!eventMod.IsLoaded) {
								continue;
							}

							if (fileEventArgs == null || fileEventSender == null) {
								return false;
							}

							return await eventMod.Module.OnConfigWatcherEventRasiedAsync(fileEventSender, fileEventArgs).ConfigureAwait(false);
						}

					case Enums.AsyncModuleContext.ModuleWatcherEvent: {
							if (!eventMod.IsLoaded) {
								continue;
							}

							if (fileEventArgs == null || fileEventSender == null) {
								return false;
							}

							return await eventMod.Module.OnModuleWatcherEventRasiedAsync(fileEventSender, fileEventArgs).ConfigureAwait(false);
						}

					case Enums.AsyncModuleContext.NetworkDisconnected: {
							if (!eventMod.IsLoaded) {
								continue;
							}

							return await eventMod.Module.OnNetworkDisconnectedAsync().ConfigureAwait(false);
						}

					case Enums.AsyncModuleContext.NetworkReconnected: {
							if (!eventMod.IsLoaded) {
								continue;
							}

							return await eventMod.Module.OnNetworkReconnectedAsync().ConfigureAwait(false);
						}

					case Enums.AsyncModuleContext.SystemRestart: {
							if (!eventMod.IsLoaded) {
								continue;
							}

							return await eventMod.Module.OnSystemRestartRequestedAsync().ConfigureAwait(false);
						}

					case Enums.AsyncModuleContext.SystemShutdown: {
							if (!eventMod.IsLoaded) {
								continue;
							}

							return await eventMod.Module.OnSystemShutdownRequestedAsync().ConfigureAwait(false);
						}

					case Enums.AsyncModuleContext.UpdateAvailable: {
							if (!eventMod.IsLoaded) {
								continue;
							}

							return await eventMod.Module.OnUpdateAvailableAsync().ConfigureAwait(false);
						}

					case Enums.AsyncModuleContext.UpdateStarted: {
							if (!eventMod.IsLoaded) {
								continue;
							}

							return await eventMod.Module.OnUpdateStartedAsync().ConfigureAwait(false);
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
