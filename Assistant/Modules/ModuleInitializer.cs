using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;
using Assistant.Modules.Interfaces;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Assistant.Modules {

	public class LoadedModules {

		public HashSet<((Enums.ModuleType, string), IEmailClient)> EmailClients { get; set; } = new HashSet<((Enums.ModuleType, string), IEmailClient)>();

		public HashSet<((Enums.ModuleType, string), IDiscordBot)> DiscordBots { get; set; } = new HashSet<((Enums.ModuleType, string), IDiscordBot)>();

		public HashSet<((Enums.ModuleType, string), ISteamClient)> SteamClients { get; set; } = new HashSet<((Enums.ModuleType, string), ISteamClient)>();

		public HashSet<((Enums.ModuleType, string), IYoutubeClient)> YoutubeClients { get; set; } = new HashSet<((Enums.ModuleType, string), IYoutubeClient)>();

		public HashSet<((Enums.ModuleType, string), ICustomModule)> CustomModules { get; set; } = new HashSet<((Enums.ModuleType, string), ICustomModule)>();

		public HashSet<Assembly> LoadedAssemblies { get; set; } = new HashSet<Assembly>();

		public bool IsModulesEmpty =>
			EmailClients.Count <= 0 && DiscordBots.Count <= 0 && SteamClients.Count <= 0 && YoutubeClients.Count <= 0 && CustomModules.Count <= 0;
	}

	public class ModuleInitializer {
		private readonly Logger Logger = new Logger("MODULES");
		public LoadedModules LoadedModules { get; private set; } = new LoadedModules();

		public bool UnloadModules(Enums.ModuleLoaderContext unloadContext) {
			switch (unloadContext) {
				case Enums.ModuleLoaderContext.AllModules: {
						if (LoadedModules.IsModulesEmpty) {
							return true;
						}

						if (LoadedModules.DiscordBots.Count > 0) {
							foreach (((Enums.ModuleType, string) UniqueId, IDiscordBot Dbot) in LoadedModules.DiscordBots) {
								Dbot.InitModuleShutdown();
								Logger.Log($"Unloaded {UniqueId} / {Dbot.ModuleVersion} module.");
							}
						}

						if (LoadedModules.EmailClients.Count > 0) {
							foreach (((Enums.ModuleType, string) UniqueId, IEmailClient Mbot) in LoadedModules.EmailClients) {
								Mbot.InitModuleShutdown();
								Logger.Log($"Unloaded {UniqueId} / {Mbot.ModuleVersion} module.");
							}
						}

						if (LoadedModules.YoutubeClients.Count > 0) {
							foreach (((Enums.ModuleType, string) UniqueId, IYoutubeClient youtube) in LoadedModules.YoutubeClients) {
								youtube.InitModuleShutdown();
								Logger.Log($"Unloaded {UniqueId} / {youtube.ModuleVersion} module.");
							}
						}

						if (LoadedModules.SteamClients.Count > 0) {
							foreach (((Enums.ModuleType, string) UniqueId, ISteamClient steam) in LoadedModules.SteamClients) {
								steam.InitModuleShutdown();
								Logger.Log($"Unloaded {UniqueId} / {steam.ModuleVersion} module.");
							}
						}

						if (LoadedModules.CustomModules.Count > 0) {
							foreach (((Enums.ModuleType, string) UniqueId, ICustomModule custom) in LoadedModules.CustomModules) {
								custom.InitModuleShutdown();
								Logger.Log($"Unloaded {UniqueId} / {custom.ModuleVersion} module.");
							}
						}

						Logger.Log("All modules have been unloaded.", Enums.LogLevels.Info);
						return true;
					}

				case Enums.ModuleLoaderContext.DiscordClients: {
						if (LoadedModules.DiscordBots.Count > 0) {
							foreach (((Enums.ModuleType, string), IDiscordBot) bot in LoadedModules.DiscordBots) {
								if (bot.Item2.StopServer().Result) {
									Logger.Log($"MODULE > {bot.Item1}/{bot.Item2.ModuleAuthor} has been unloaded.", Enums.LogLevels.Trace);
								}
							}
						}
						return true;
					}

				case Enums.ModuleLoaderContext.EmailClients: {
						if (LoadedModules.EmailClients.Count > 0) {
							foreach (((Enums.ModuleType, string), IEmailClient) bot in LoadedModules.EmailClients) {
								if (bot.Item2.DisposeAllEmailBots()) {
									Logger.Log($"MODULE > {bot.Item1}/{bot.Item2.ModuleAuthor} has been unloaded.", Enums.LogLevels.Trace);
								}
							}
						}
						return true;
					}

				case Enums.ModuleLoaderContext.SteamClients: {
						if (LoadedModules.SteamClients.Count > 0) {
							foreach (((Enums.ModuleType, string), ISteamClient) bot in LoadedModules.SteamClients) {
								if (bot.Item2.DisposeAllSteamBots()) {
									Logger.Log($"MODULE > {bot.Item1}/{bot.Item2.ModuleAuthor} has been unloaded.", Enums.LogLevels.Trace);
								}
							}
						}
						return true;
					}

				case Enums.ModuleLoaderContext.YoutubeClients: {
						if (LoadedModules.YoutubeClients.Count > 0) {
							foreach (((Enums.ModuleType, string), IYoutubeClient) bot in LoadedModules.YoutubeClients) {
								if (bot.Item2.InitModuleShutdown()) {
									Logger.Log($"MODULE > {bot.Item1}/{bot.Item2.ModuleAuthor} has been unloaded.", Enums.LogLevels.Trace);
								}
							}
						}
						return true;
					}

				case Enums.ModuleLoaderContext.CustomModules: {
						if (LoadedModules.CustomModules.Count > 0) {
							foreach (((Enums.ModuleType, string), ICustomModule) bot in LoadedModules.CustomModules) {
								if (bot.Item2.InitModuleShutdown()) {
									Logger.Log($"MODULE > {bot.Item1}/{bot.Item2.ModuleAuthor} has been unloaded.", Enums.LogLevels.Trace);
								}
							}
						}
						return true;
					}

				default:
					return false;
			}
		}

		private bool IsExisitingModule<T>(Enums.ModuleLoaderContext context, T module) where T : IModuleBase {
			if (module == null) {
				return false;
			}

			switch (context) {
				case Enums.ModuleLoaderContext.DiscordClients: {
						IDiscordBot bot = (IDiscordBot) module;

						if (LoadedModules.DiscordBots.Count <= 0) {
							return false;
						}

						foreach (((Enums.ModuleType, string), IDiscordBot) mod in LoadedModules.DiscordBots) {
							if (mod.Item1.Equals(bot.ModuleIdentifier)) {
								Logger.Log("This module is already loaded and added to module collection.", Enums.LogLevels.Trace);
								return true;
							}
						}

						return false;
					}

				case Enums.ModuleLoaderContext.EmailClients: {
						IEmailClient bot = (IEmailClient) module;

						if (LoadedModules.EmailClients.Count <= 0) {
							return false;
						}

						foreach (((Enums.ModuleType, string), IEmailClient) mod in LoadedModules.EmailClients) {
							if (mod.Item1.Equals(bot.ModuleIdentifier)) {
								Logger.Log("This module is already loaded and added to module collection.", Enums.LogLevels.Trace);
								return true;
							}
						}

						return false;
					}

				case Enums.ModuleLoaderContext.SteamClients: {
						ISteamClient bot = (ISteamClient) module;

						if (LoadedModules.SteamClients.Count <= 0) {
							return false;
						}

						foreach (((Enums.ModuleType, string), ISteamClient) mod in LoadedModules.SteamClients) {
							if (mod.Item1.Equals(bot.ModuleIdentifier)) {
								Logger.Log("This module is already loaded and added to module collection.", Enums.LogLevels.Trace);
								return true;
							}
						}

						return false;
					}

				case Enums.ModuleLoaderContext.YoutubeClients: {
						IYoutubeClient bot = (IYoutubeClient) module;

						if (LoadedModules.YoutubeClients.Count <= 0) {
							return false;
						}

						foreach (((Enums.ModuleType, string), IYoutubeClient) mod in LoadedModules.YoutubeClients) {
							if (mod.Item1.Equals(bot.ModuleIdentifier)) {
								Logger.Log("This module is already loaded and added to module collection.", Enums.LogLevels.Trace);
								return true;
							}
						}

						return false;
					}

				case Enums.ModuleLoaderContext.CustomModules: {
						ICustomModule bot = (ICustomModule) module;

						if (LoadedModules.CustomModules.Count <= 0) {
							return false;
						}

						foreach (((Enums.ModuleType, string), ICustomModule) mod in LoadedModules.CustomModules) {
							if (mod.Item1.Equals(bot.ModuleIdentifier)) {
								Logger.Log("This module is already loaded and added to module collection.", Enums.LogLevels.Trace);
								return true;
							}
						}

						return false;
					}
				default: {
						return true;
					}
			}
		}

		[NotNull]
		public (bool, LoadedModules) LoadModules(Enums.ModuleLoaderContext loadContext = Enums.ModuleLoaderContext.AllModules, bool moduleWatcherInvoke = false) {
			if (!moduleWatcherInvoke && !LoadedModules.IsModulesEmpty) {
				return (false, LoadedModules);
			}

			LoadedModules.LoadedAssemblies = LoadAssemblies();

			if ((LoadedModules.LoadedAssemblies == null) || (LoadedModules.LoadedAssemblies.Count == 0)) {
				Logger.Log("No modules found.", Enums.LogLevels.Trace);
				return (false, LoadedModules);
			}

			Logger.Log("Loading modules...", Enums.LogLevels.Trace);

			switch (loadContext) {
				case Enums.ModuleLoaderContext.AllModules:
					if (Load<IEmailClient>(Enums.ModuleLoaderContext.EmailClients)) {
						Logger.Log("Finished loading [EMAIL] modules.", Enums.LogLevels.Trace);
					}

					if (Load<IDiscordBot>(Enums.ModuleLoaderContext.DiscordClients)) {
						Logger.Log("Finished loading [DISCORD] modules.", Enums.LogLevels.Trace);
					}

					if (Load<ISteamClient>(Enums.ModuleLoaderContext.SteamClients)) {
						Logger.Log("Finished loading [STEAM] modules.", Enums.LogLevels.Trace);
					}

					if (Load<IYoutubeClient>(Enums.ModuleLoaderContext.YoutubeClients)) {
						Logger.Log("Finished loading [YOUTUBE] modules.", Enums.LogLevels.Trace);
					}

					if (Load<ICustomModule>(Enums.ModuleLoaderContext.CustomModules)) {
						Logger.Log("Finished loading [CUSTOM] modules.", Enums.LogLevels.Trace);
					}

					break;

				case Enums.ModuleLoaderContext.DiscordClients:
					if (Load<IDiscordBot>(loadContext)) {
						Logger.Log("Finished loading [DISCORD] modules.", Enums.LogLevels.Trace);
					}

					break;

				case Enums.ModuleLoaderContext.EmailClients:
					if (Load<IEmailClient>(loadContext)) {
						Logger.Log("Finished loading [EMAIL] modules.", Enums.LogLevels.Trace);
					}

					break;

				case Enums.ModuleLoaderContext.SteamClients:
					if (Load<ISteamClient>(loadContext)) {
						Logger.Log("Finished loading [STEAM] modules.", Enums.LogLevels.Trace);
					}

					break;

				case Enums.ModuleLoaderContext.YoutubeClients:
					if (Load<IYoutubeClient>(loadContext)) {
						Logger.Log("Finished loading [YOUTUBE] modules.", Enums.LogLevels.Trace);
					}

					break;

				case Enums.ModuleLoaderContext.CustomModules:
					if (Load<ICustomModule>(loadContext)) {
						Logger.Log("Finished loading [CUSTOM] modules.", Enums.LogLevels.Trace);
					}

					break;

				case Enums.ModuleLoaderContext.None:
					Logger.Log("Loader context is set to load none of modules. therefore, aborting the loading process.");
					break;
			}

			return (true, LoadedModules);
		}

		private bool InitLoadedModuleService<T>(HashSet<((Enums.ModuleType, string), T)> moduleCollection, Enums.ModuleLoaderContext loadContext) where T : IModuleBase {
			if (moduleCollection == null || moduleCollection.Count == 0) {
				return false;
			}

			switch (loadContext) {
				case Enums.ModuleLoaderContext.DiscordClients: {
						int loadedModuleCount = 0;
						foreach (((Enums.ModuleType, string), T) value in moduleCollection) {
							IDiscordBot plugin = (IDiscordBot) value.Item2;
							(Enums.ModuleType, string) uniqueId = value.Item1;

							try {
								Logger.Log($"Loading [DISCORD] {plugin.ModuleIdentifier} module...", Enums.LogLevels.Trace);

								if (!IsExisitingModule(loadContext, plugin)) {
									if (plugin.InitModuleService()) {
										LoadedModules.DiscordBots.Add((uniqueId, plugin));
										Logger.Log($"Load successfull. [DISCORD] {plugin.ModuleIdentifier} / {plugin.ModuleAuthor} / V{plugin.ModuleVersion}", Enums.LogLevels.Info);
										loadedModuleCount++;
									}
								}
								else {
									Logger.Log($"Cancelled loading [DISCORD] {uniqueId} / {plugin.ModuleAuthor} / V{plugin.ModuleVersion} module as its already loaded.", Enums.LogLevels.Info);
								}
							}
							catch (Exception e) {
								Logger.Log(e);
							}
						}
						Logger.Log($"Successfully loaded {loadedModuleCount} Discord clients. ({loadedModuleCount}/{moduleCollection.Count - loadedModuleCount})", Enums.LogLevels.Trace);
					}
					break;

				case Enums.ModuleLoaderContext.EmailClients: {
						int loadedModuleCount = 0;
						foreach (((Enums.ModuleType, string), T) value in moduleCollection) {
							IEmailClient plugin = (IEmailClient) value.Item2;
							(Enums.ModuleType, string) uniqueId = value.Item1;

							try {
								Logger.Log($"Loading [EMAIL] {plugin.ModuleIdentifier} module...", Enums.LogLevels.Trace);

								if (!IsExisitingModule(loadContext, plugin)) {
									if (plugin.InitModuleService()) {
										LoadedModules.EmailClients.Add((uniqueId, plugin));
										Logger.Log($"Load successfull. [EMAIL] {plugin.ModuleIdentifier} / {plugin.ModuleAuthor} / V{plugin.ModuleVersion}", Enums.LogLevels.Info);
										loadedModuleCount++;
									}
								}
								else {
									Logger.Log($"Cancelled loading [EMAIL] {uniqueId} / {plugin.ModuleAuthor} / V{plugin.ModuleVersion} module as its already loaded.", Enums.LogLevels.Info);
								}
							}
							catch (Exception e) {
								Logger.Log(e);
							}
						}
						Logger.Log($"Successfully loaded {loadedModuleCount} Email clients. ({loadedModuleCount}/{moduleCollection.Count - loadedModuleCount})", Enums.LogLevels.Trace);
					}
					break;

				case Enums.ModuleLoaderContext.SteamClients: {
						int loadedModuleCount = 0;
						foreach (((Enums.ModuleType, string), T) value in moduleCollection) {
							ISteamClient plugin = (ISteamClient) value.Item2;
							(Enums.ModuleType, string) uniqueId = value.Item1;

							try {
								Logger.Log($"Loading [STEAM] {plugin.ModuleIdentifier} module...", Enums.LogLevels.Trace);

								if (!IsExisitingModule(loadContext, plugin)) {
									if (plugin.InitModuleService()) {
										LoadedModules.SteamClients.Add((uniqueId, plugin));
										Logger.Log($"Load successfull. [STEAM] {plugin.ModuleIdentifier} / {plugin.ModuleAuthor} / V{plugin.ModuleVersion}", Enums.LogLevels.Info);
										loadedModuleCount++;
									}
								}
								else {
									Logger.Log($"Cancelled loading [STEAM] {uniqueId} / {plugin.ModuleAuthor} / V{plugin.ModuleVersion} module as its already loaded.", Enums.LogLevels.Info);
								}
							}
							catch (Exception e) {
								Logger.Log(e);
							}
						}
						Logger.Log($"Successfully loaded {loadedModuleCount} Steam clients. ({loadedModuleCount}/{moduleCollection.Count - loadedModuleCount})", Enums.LogLevels.Trace);
					}
					break;

				case Enums.ModuleLoaderContext.YoutubeClients: {
						int loadedModuleCount = 0;
						foreach (((Enums.ModuleType, string), T) value in moduleCollection) {
							IYoutubeClient plugin = (IYoutubeClient) value.Item2;
							(Enums.ModuleType, string) uniqueId = value.Item1;

							try {
								Logger.Log($"Loading [YOUTUBE] {plugin.ModuleIdentifier} module...", Enums.LogLevels.Trace);

								if (!IsExisitingModule(loadContext, plugin)) {
									if (plugin.InitModuleService()) {
										LoadedModules.YoutubeClients.Add((uniqueId, plugin));
										Logger.Log($"Load successfull. [YOUTUBE] {plugin.ModuleIdentifier} / {plugin.ModuleAuthor} / V{plugin.ModuleVersion}", Enums.LogLevels.Info);
										loadedModuleCount++;
									}
								}
								else {
									Logger.Log($"Cancelled loading [YOUTUBE] {uniqueId} / {plugin.ModuleAuthor} / V{plugin.ModuleVersion} module as its already loaded.", Enums.LogLevels.Info);
								}
							}
							catch (Exception e) {
								Logger.Log(e);
							}
						}
						Logger.Log($"Successfully loaded {loadedModuleCount} Youtube clients. ({loadedModuleCount}/{moduleCollection.Count - loadedModuleCount})", Enums.LogLevels.Trace);
					}
					break;

				case Enums.ModuleLoaderContext.CustomModules: {
						int loadedModuleCount = 0;
						foreach (((Enums.ModuleType, string), T) value in moduleCollection) {
							ICustomModule plugin = (ICustomModule) value.Item2;
							(Enums.ModuleType, string) uniqueId = value.Item1;

							try {
								Logger.Log($"Loading [CUSTOM] {plugin.ModuleIdentifier} module...", Enums.LogLevels.Trace);

								if (!IsExisitingModule(loadContext, plugin)) {
									if (plugin.InitModuleService()) {
										LoadedModules.CustomModules.Add((uniqueId, plugin));
										Logger.Log($"Load successfull. [CUSTOM] {plugin.ModuleIdentifier} / {plugin.ModuleAuthor} / V{plugin.ModuleVersion}", Enums.LogLevels.Info);
										loadedModuleCount++;
									}
								}
								else {
									Logger.Log($"Cancelled loading [CUSTOM] {uniqueId} / {plugin.ModuleAuthor} / V{plugin.ModuleVersion} module as its already loaded.", Enums.LogLevels.Info);
								}
							}
							catch (Exception e) {
								Logger.Log(e);
							}
						}
						Logger.Log($"Successfully loaded {loadedModuleCount} Custom modules. ({loadedModuleCount}/{moduleCollection.Count - loadedModuleCount})", Enums.LogLevels.Trace);
					}
					break;

				default: {
						return false;
					}
			}

			return true;
		}

		[NotNull]
		private bool Load<T>(Enums.ModuleLoaderContext context) where T : IModuleBase {
			ConventionBuilder conventions = new ConventionBuilder();
			conventions.ForTypesDerivedFrom<T>().Export<T>();
			ContainerConfiguration configuration = new ContainerConfiguration().WithAssemblies(LoadedModules.LoadedAssemblies, conventions);

			switch (context) {
				case Enums.ModuleLoaderContext.DiscordClients: {
						HashSet<((Enums.ModuleType, string), IDiscordBot)> DiscordModules = new HashSet<((Enums.ModuleType, string), IDiscordBot)>();
						try {
							using (CompositionHost container = configuration.CreateContainer()) {
								HashSet<IDiscordBot> hashSet = container.GetExports<IDiscordBot>().ToHashSet();

								if (hashSet.Count > 0) {
									foreach (IDiscordBot bot in hashSet) {
										bot.ModuleIdentifier = GenerateModuleIdentifier(Enums.ModuleType.Discord);
										DiscordModules.Add((bot.ModuleIdentifier, bot));
										Logger.Log($"Added {bot.ModuleIdentifier}/{bot.ModuleAuthor}/{bot.ModuleVersion} to modules collection.", Enums.LogLevels.Trace);
									}
								}
							}
						}
						catch (Exception e) {
							Logger.Log(e);
							return false;
						}

						if (DiscordModules.Count <= 0) {
							return false;
						}

						return InitLoadedModuleService<IDiscordBot>(DiscordModules, Enums.ModuleLoaderContext.DiscordClients);
					}

				case Enums.ModuleLoaderContext.EmailClients: {
						HashSet<((Enums.ModuleType, string), IEmailClient)> EmailModules = new HashSet<((Enums.ModuleType, string), IEmailClient)>();
						try {
							using (CompositionHost container = configuration.CreateContainer()) {
								HashSet<IEmailClient> hashSet = container.GetExports<IEmailClient>().ToHashSet();

								if (hashSet.Count > 0) {
									foreach (IEmailClient bot in hashSet) {
										bot.ModuleIdentifier = GenerateModuleIdentifier(Enums.ModuleType.Email);
										EmailModules.Add((bot.ModuleIdentifier, bot));
										Logger.Log($"Added {bot.ModuleIdentifier}/{bot.ModuleAuthor}/{bot.ModuleVersion} to modules collection", Enums.LogLevels.Trace);
									}
								}
							}
						}
						catch (Exception e) {
							Logger.Log(e);
							return false;
						}

						if (EmailModules.Count <= 0) {
							return false;
						}

						return InitLoadedModuleService<IEmailClient>(EmailModules, Enums.ModuleLoaderContext.EmailClients);
					}

				case Enums.ModuleLoaderContext.YoutubeClients: {
						HashSet<((Enums.ModuleType, string), IYoutubeClient)> YoutubeModules = new HashSet<((Enums.ModuleType, string), IYoutubeClient)>();
						try {
							using (CompositionHost container = configuration.CreateContainer()) {
								HashSet<IYoutubeClient> hashSet = container.GetExports<IYoutubeClient>().ToHashSet();

								if (hashSet.Count > 0) {
									foreach (IYoutubeClient bot in hashSet) {
										bot.ModuleIdentifier = GenerateModuleIdentifier(Enums.ModuleType.Youtube);
										YoutubeModules.Add((bot.ModuleIdentifier, bot));
										Logger.Log($"Added {bot.ModuleIdentifier}/{bot.ModuleAuthor}/{bot.ModuleVersion} to modules collection", Enums.LogLevels.Trace);
									}
								}
							}
						}
						catch (Exception e) {
							Logger.Log(e);
							return false;
						}

						if (YoutubeModules.Count <= 0) {
							return false;
						}

						return InitLoadedModuleService<IYoutubeClient>(YoutubeModules, Enums.ModuleLoaderContext.YoutubeClients);
					}

				case Enums.ModuleLoaderContext.SteamClients: {
						HashSet<((Enums.ModuleType, string), ISteamClient)> SteamModules = new HashSet<((Enums.ModuleType, string), ISteamClient)>();
						try {
							using (CompositionHost container = configuration.CreateContainer()) {
								HashSet<ISteamClient> hashSet = container.GetExports<ISteamClient>().ToHashSet();

								if (hashSet.Count > 0) {
									foreach (ISteamClient bot in hashSet) {
										bot.ModuleIdentifier = GenerateModuleIdentifier(Enums.ModuleType.Steam);
										SteamModules.Add((bot.ModuleIdentifier, bot));
										Logger.Log($"Added {bot.ModuleIdentifier}/{bot.ModuleAuthor}/{bot.ModuleVersion} to modules collection", Enums.LogLevels.Trace);
									}
								}
							}
						}
						catch (Exception e) {
							Logger.Log(e);
							return false;
						}

						if (SteamModules.Count <= 0) {
							return false;
						}

						return InitLoadedModuleService<ISteamClient>(SteamModules, Enums.ModuleLoaderContext.SteamClients);
					}

				case Enums.ModuleLoaderContext.CustomModules: {
						HashSet<((Enums.ModuleType, string), ICustomModule)> CustomModules = new HashSet<((Enums.ModuleType, string), ICustomModule)>();
						try {
							using (CompositionHost container = configuration.CreateContainer()) {
								HashSet<ICustomModule> hashSet = container.GetExports<ICustomModule>().ToHashSet();

								if (hashSet.Count > 0) {
									foreach (ICustomModule bot in hashSet) {
										bot.ModuleIdentifier = GenerateModuleIdentifier(Enums.ModuleType.Steam);
										CustomModules.Add((bot.ModuleIdentifier, bot));
										Logger.Log($"Added {bot.ModuleIdentifier}/{bot.ModuleAuthor}/{bot.ModuleVersion} to modules collection", Enums.LogLevels.Trace);
									}
								}
							}
						}
						catch (Exception e) {
							Logger.Log(e);
							return false;
						}

						if (CustomModules.Count <= 0) {
							return false;
						}

						return InitLoadedModuleService<ICustomModule>(CustomModules, Enums.ModuleLoaderContext.CustomModules);
					}

				default:
					Logger.Log($"Unknown type of plugin loaded. cannot load with assistant.", Enums.LogLevels.Warn);
					return false;
			}
		}

		[NotNull]
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

		[NotNull]
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

		public bool OnCoreShutdown() {
			if (UnloadModules(Enums.ModuleLoaderContext.AllModules)) {
				Logger.Log("All modules have been dissconnected and shutdown!", Enums.LogLevels.Trace);
				return true;
			}

			Logger.Log("Failed to shutdown modules.", Enums.LogLevels.Warn);
			return false;
		}

		private (Enums.ModuleType, string) GenerateModuleIdentifier(Enums.ModuleType context) {
			StringBuilder builder = new StringBuilder();
			Enumerable
				.Range(65, 26)
				.Select(e => ((char) e).ToString())
				.Concat(Enumerable.Range(97, 26).Select(e => ((char) e).ToString()))
				.Concat(Enumerable.Range(0, 10).Select(e => e.ToString()))
				.OrderBy(e => Guid.NewGuid())
				.Take(11)
				.ToList().ForEach(e => builder.Append(e));
			return (context, builder.ToString());
		}
	}
}
