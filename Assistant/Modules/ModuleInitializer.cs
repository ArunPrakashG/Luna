using HomeAssistant.Extensions;
using HomeAssistant.Log;
using HomeAssistant.Modules.Interfaces;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HomeAssistant.AssistantCore;

namespace HomeAssistant.Modules {

	public class LoadedModules {

		public HashSet<(long, IEmailClient)> EmailClients { get; set; } = new HashSet<(long, IEmailClient)>();

		public HashSet<(long, IDiscordBot)> DiscordBots { get; set; } = new HashSet<(long, IDiscordBot)>();

		public HashSet<(long, IGoogleMap)> GoogleMapModules { get; set; } = new HashSet<(long, IGoogleMap)>();

		public HashSet<(long, ISteamClient)> SteamClients { get; set; } = new HashSet<(long, ISteamClient)>();

		public HashSet<(long, IYoutubeClient)> YoutubeClients { get; set; } = new HashSet<(long, IYoutubeClient)>();

		public HashSet<(long, ILoggerBase)> LoggerBase { get; set; } = new HashSet<(long, ILoggerBase)>();

		public HashSet<(long, IMiscModule)> MiscModules { get; set; } = new HashSet<(long, IMiscModule)>();

		public HashSet<Assembly> LoadedAssemblies { get; set; } = new HashSet<Assembly>();

		public bool IsModulesEmpty =>
			EmailClients.Count <= 0 && DiscordBots.Count <= 0 && GoogleMapModules.Count <= 0 &&
			SteamClients.Count <= 0 && YoutubeClients.Count <= 0 && LoggerBase.Count <= 0;
	}

	public class ModuleInitializer {
		private readonly Logger Logger = new Logger("MODULES");

		public LoadedModules LoadedModules { get; private set; } = new LoadedModules();
		private readonly Random Random = new Random();

		public bool UnloadModules(Enums.ModuleLoaderContext unloadContext) {
			switch (unloadContext) {
				case Enums.ModuleLoaderContext.All: {
						return OnCoreShutdown().Result;
					}

				case Enums.ModuleLoaderContext.DiscordClients: {
						if (LoadedModules.DiscordBots.Count > 0) {
							foreach ((long, IDiscordBot) bot in LoadedModules.DiscordBots) {
								if (bot.Item2.StopServer().Result) {
									Logger.Log($"BOT > {bot.Item1}/{bot.Item2.ModuleAuthor} has been stopped.", Enums.LogLevels.Trace);
								}
							}
						}
						return true;
					}

				case Enums.ModuleLoaderContext.EmailClients: {
						if (LoadedModules.EmailClients.Count > 0) {
							foreach ((long, IEmailClient) bot in LoadedModules.EmailClients) {
								if (bot.Item2.DisposeAllEmailBots()) {
									Logger.Log($"BOT > {bot.Item1}/{bot.Item2.ModuleAuthor} has been stopped.", Enums.LogLevels.Trace);
								}
							}
						}
						return true;
					}

				case Enums.ModuleLoaderContext.GoogleMaps: {
						if (LoadedModules.GoogleMapModules.Count > 0) {
							foreach ((long, IGoogleMap) bot in LoadedModules.GoogleMapModules) {
								if (bot.Item2.InitModuleShutdown()) {
									Logger.Log($"BOT > {bot.Item1}/{bot.Item2.ModuleAuthor} has been stopped.", Enums.LogLevels.Trace);
								}
							}
						}
						return true;
					}

				case Enums.ModuleLoaderContext.MiscModules: {
						if (LoadedModules.MiscModules.Count > 0) {
							foreach ((long, IMiscModule) bot in LoadedModules.MiscModules) {
								if (bot.Item2.InitModuleShutdown()) {
									Logger.Log($"BOT > {bot.Item1}/{bot.Item2.ModuleAuthor} has been stopped.", Enums.LogLevels.Trace);
								}
							}
						}
						return true;
					}

				case Enums.ModuleLoaderContext.SteamClients: {
						if (LoadedModules.SteamClients.Count > 0) {
							foreach ((long, ISteamClient) bot in LoadedModules.SteamClients) {
								if (bot.Item2.DisposeAllBots()) {
									Logger.Log($"BOT > {bot.Item1}/{bot.Item2.ModuleAuthor} has been stopped.", Enums.LogLevels.Trace);
								}
							}
						}
						return true;
					}

				case Enums.ModuleLoaderContext.YoutubeClients: {
						if (LoadedModules.YoutubeClients.Count > 0) {
							foreach ((long, IYoutubeClient) bot in LoadedModules.YoutubeClients) {
								if (bot.Item2.InitModuleShutdown()) {
									Logger.Log($"BOT > {bot.Item1}/{bot.Item2.ModuleAuthor} has been stopped.", Enums.LogLevels.Trace);
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

						foreach ((long, IDiscordBot) mod in LoadedModules.DiscordBots) {
							if (mod.Item1.Equals(bot.ModuleIdentifier)) {
								Logger.Log("This bot already exists in assistant bot collection. cannot load.", Enums.LogLevels.Trace);
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

						foreach ((long, IEmailClient) mod in LoadedModules.EmailClients) {
							if (mod.Item1.Equals(bot.ModuleIdentifier)) {
								Logger.Log("This bot already exists in assistant bot collection. cannot load.", Enums.LogLevels.Trace);
								return true;
							}
						}

						return false;
					}

				case Enums.ModuleLoaderContext.GoogleMaps: {
						IGoogleMap bot = (IGoogleMap) module;

						if (LoadedModules.GoogleMapModules.Count <= 0) {
							return false;
						}

						foreach ((long, IGoogleMap) mod in LoadedModules.GoogleMapModules) {
							if (mod.Item1.Equals(bot.ModuleIdentifier)) {
								Logger.Log("This bot already exists in assistant bot collection. cannot load.", Enums.LogLevels.Trace);
								return true;
							}
						}

						return false;
					}

				case Enums.ModuleLoaderContext.MiscModules: {
						IMiscModule bot = (IMiscModule) module;

						if (LoadedModules.MiscModules.Count <= 0) {
							return false;
						}

						foreach ((long, IMiscModule) mod in LoadedModules.MiscModules) {
							if (mod.Item1.Equals(bot.ModuleIdentifier)) {
								Logger.Log("This bot already exists in assistant bot collection. cannot load.", Enums.LogLevels.Trace);
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

						foreach ((long, ISteamClient) mod in LoadedModules.SteamClients) {
							if (mod.Item1.Equals(bot.ModuleIdentifier)) {
								Logger.Log("This bot already exists in assistant bot collection. cannot load.", Enums.LogLevels.Trace);
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

						foreach ((long, IYoutubeClient) mod in LoadedModules.YoutubeClients) {
							if (mod.Item1.Equals(bot.ModuleIdentifier)) {
								Logger.Log("This bot already exists in assistant bot collection. cannot load.", Enums.LogLevels.Trace);
								return true;
							}
						}

						return false;
					}

				case Enums.ModuleLoaderContext.Logger: {
						ILoggerBase bot = (ILoggerBase) module;

						if (LoadedModules.LoggerBase.Count <= 0) {
							return false;
						}

						foreach ((long, ILoggerBase) mod in LoadedModules.LoggerBase) {
							if (mod.Item1.Equals(bot.ModuleIdentifier)) {
								Logger.Log("This bot already exists in assistant bot collection. cannot load.", Enums.LogLevels.Trace);
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
		public (bool, LoadedModules) LoadModules(Enums.ModuleLoaderContext loadContext = Enums.ModuleLoaderContext.All, bool moduleWatcherInvoke = false) {
			if (!moduleWatcherInvoke && !LoadedModules.IsModulesEmpty) {
				return (false, LoadedModules);
			}

			LoadedModules.LoadedAssemblies = LoadAssemblies();

			if ((LoadedModules.LoadedAssemblies == null) || (LoadedModules.LoadedAssemblies.Count == 0)) {
				Logger.Log("No modules found.", Enums.LogLevels.Error);
				return (false, LoadedModules);
			}

			Logger.Log("Loading modules...", Enums.LogLevels.Trace);

			switch (loadContext) {
				case Enums.ModuleLoaderContext.All:
					if (Load<IEmailClient>(Enums.ModulesContext.Email)) {
						Logger.Log("All Email modules have been loaded.", Enums.LogLevels.Trace);
					}

					if (Load<IDiscordBot>(Enums.ModulesContext.Discord)) {
						Logger.Log("Discord bot has been loaded.", Enums.LogLevels.Trace);
					}

					if (Load<ISteamClient>(Enums.ModulesContext.Steam)) {
						Logger.Log("Steam client has been loaded.", Enums.LogLevels.Trace);
					}

					if (Load<IGoogleMap>(Enums.ModulesContext.GoogleMap)) {
						Logger.Log("Google map serivces has been loaded.", Enums.LogLevels.Trace);
					}

					if (Load<IYoutubeClient>(Enums.ModulesContext.Youtube)) {
						Logger.Log("Youtube client has been loaded.", Enums.LogLevels.Trace);
					}

					if (Load<IMiscModule>(Enums.ModulesContext.Misc)) {
						Logger.Log("Misc modules have been loaded.", Enums.LogLevels.Trace);
					}

					break;

				case Enums.ModuleLoaderContext.DiscordClients:
					if (Load<IDiscordBot>(Enums.ModulesContext.Discord)) {
						Logger.Log("Discord bot has been loaded.", Enums.LogLevels.Trace);
					}

					break;

				case Enums.ModuleLoaderContext.EmailClients:
					if (Load<IEmailClient>(Enums.ModulesContext.Email)) {
						Logger.Log("All Email modules have been loaded.", Enums.LogLevels.Trace);
					}

					break;

				case Enums.ModuleLoaderContext.GoogleMaps:
					if (Load<IGoogleMap>(Enums.ModulesContext.GoogleMap)) {
						Logger.Log("Google map serivces has been loaded.", Enums.LogLevels.Trace);
					}

					break;

				case Enums.ModuleLoaderContext.MiscModules:
					if (Load<IMiscModule>(Enums.ModulesContext.Misc)) {
						Logger.Log("Misc modules have been loaded.", Enums.LogLevels.Trace);
					}

					break;

				case Enums.ModuleLoaderContext.SteamClients:
					if (Load<ISteamClient>(Enums.ModulesContext.Steam)) {
						Logger.Log("Steam client has been loaded.", Enums.LogLevels.Trace);
					}

					break;

				case Enums.ModuleLoaderContext.YoutubeClients:
					if (Load<IYoutubeClient>(Enums.ModulesContext.Youtube)) {
						Logger.Log("Youtube client has been loaded.", Enums.LogLevels.Trace);
					}

					break;

				case Enums.ModuleLoaderContext.None:
					Logger.Log("Loader context is set to load none of modules. therefore, aborting the loading process.");
					break;
			}

			return (true, LoadedModules);
		}

		[NotNull]
		private bool Load<T>(Enums.ModulesContext context) where T : IModuleBase {
			ConventionBuilder conventions = new ConventionBuilder();
			conventions.ForTypesDerivedFrom<T>().Export<T>();
			ContainerConfiguration configuration = new ContainerConfiguration().WithAssemblies(LoadedModules.LoadedAssemblies, conventions);

			switch (context) {
				case Enums.ModulesContext.Discord: {
						HashSet<(long, IDiscordBot)> DiscordModules = new HashSet<(long, IDiscordBot)>();
						try {
							using (CompositionHost container = configuration.CreateContainer()) {
								HashSet<IDiscordBot> hashSet = container.GetExports<IDiscordBot>().ToHashSet();

								if (hashSet.Count > 0) {
									foreach (IDiscordBot bot in hashSet) {
										bot.ModuleIdentifier = GenerateModuleIdentifier();
										DiscordModules.Add((bot.ModuleIdentifier, bot));
										Logger.Log($"Added bot {bot.ModuleIdentifier}/{bot.ModuleAuthor}/{bot.ModuleVersion} to modules list", Enums.LogLevels.Trace);
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

						foreach ((long uniqueId, IDiscordBot plugin) in DiscordModules) {
							try {
								Logger.Log($"Loading {plugin.ModuleIdentifier} module.", Enums.LogLevels.Trace);

								if (!IsExisitingModule(Enums.ModuleLoaderContext.DiscordClients, plugin)) {
									if (plugin.InitModuleService()) {
										LoadedModules.DiscordBots.Add((uniqueId, plugin));
										Logger.Log($"Loaded {plugin.ModuleIdentifier} module by {plugin.ModuleAuthor} | v{plugin.ModuleVersion}!", Enums.LogLevels.Trace);
									}
								}
								else {
									Logger.Log($"Not loading {uniqueId}/{plugin.ModuleAuthor}/{plugin.ModuleVersion} module as it already exists!", Enums.LogLevels.Trace);
								}
							}
							catch (Exception e) {
								Logger.Log(e);
							}
						}

						return true;
					}

				case Enums.ModulesContext.Email: {
						HashSet<(long, IEmailClient)> EmailModules = new HashSet<(long, IEmailClient)>();
						try {
							using (CompositionHost container = configuration.CreateContainer()) {
								HashSet<IEmailClient> hashSet = container.GetExports<IEmailClient>().ToHashSet();

								if (hashSet.Count > 0) {
									foreach (IEmailClient bot in hashSet) {
										bot.ModuleIdentifier = GenerateModuleIdentifier();
										EmailModules.Add((bot.ModuleIdentifier, bot));
										Logger.Log($"Added bot {bot.ModuleIdentifier}/{bot.ModuleAuthor}/{bot.ModuleVersion} to modules list", Enums.LogLevels.Trace);
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

						foreach ((long uniqueId, IEmailClient plugin) in EmailModules) {
							try {
								Logger.Log($"Loading {plugin.ModuleIdentifier} module.", Enums.LogLevels.Trace);

								if (!IsExisitingModule(Enums.ModuleLoaderContext.EmailClients, plugin)) {
									if (plugin.InitModuleService()) {
										LoadedModules.EmailClients.Add((uniqueId, plugin));
										Logger.Log($"Loaded {plugin.ModuleIdentifier} module by {plugin.ModuleAuthor} | v{plugin.ModuleVersion}!", Enums.LogLevels.Trace);
									}
								}
								else {
									Logger.Log($"Not loading {uniqueId}/{plugin.ModuleAuthor}/{plugin.ModuleVersion} module as it already exists!", Enums.LogLevels.Trace);
								}
							}
							catch (Exception e) {
								Logger.Log(e);
							}
						}

						return true;
					}

				case Enums.ModulesContext.GoogleMap: {
						HashSet<(long, IGoogleMap)> MapModules = new HashSet<(long, IGoogleMap)>();
						try {
							using (CompositionHost container = configuration.CreateContainer()) {
								HashSet<IGoogleMap> hashSet = container.GetExports<IGoogleMap>().ToHashSet();

								if (hashSet.Count > 0) {
									foreach (IGoogleMap bot in hashSet) {
										bot.ModuleIdentifier = GenerateModuleIdentifier();
										MapModules.Add((bot.ModuleIdentifier, bot));
										Logger.Log($"Added bot {bot.ModuleIdentifier}/{bot.ModuleAuthor}/{bot.ModuleVersion} to modules list", Enums.LogLevels.Trace);
									}
								}
							}
						}
						catch (Exception e) {
							Logger.Log(e);
							return false;
						}

						if (MapModules.Count <= 0) {
							return false;
						}

						foreach ((long uniqueId, IGoogleMap plugin) in MapModules) {
							try {
								Logger.Log($"Loading {plugin.ModuleIdentifier} module.", Enums.LogLevels.Trace);

								if (!IsExisitingModule(Enums.ModuleLoaderContext.GoogleMaps, plugin)) {
									if (plugin.InitModuleService()) {
										LoadedModules.GoogleMapModules.Add((uniqueId, plugin));
										Logger.Log($"Loaded {plugin.ModuleIdentifier} module by {plugin.ModuleAuthor} | v{plugin.ModuleVersion}!", Enums.LogLevels.Trace);
									}
								}
								else {
									Logger.Log($"Not loading {uniqueId}/{plugin.ModuleAuthor}/{plugin.ModuleVersion} module as it already exists!", Enums.LogLevels.Trace);
								}
							}
							catch (Exception e) {
								Logger.Log(e);
							}
						}

						return true;
					}

				case Enums.ModulesContext.Youtube: {
						HashSet<(long, IYoutubeClient)> YoutubeModules = new HashSet<(long, IYoutubeClient)>();
						try {
							using (CompositionHost container = configuration.CreateContainer()) {
								HashSet<IYoutubeClient> hashSet = container.GetExports<IYoutubeClient>().ToHashSet();

								if (hashSet.Count > 0) {
									foreach (IYoutubeClient bot in hashSet) {
										bot.ModuleIdentifier = GenerateModuleIdentifier();
										YoutubeModules.Add((bot.ModuleIdentifier, bot));
										Logger.Log($"Added bot {bot.ModuleIdentifier}/{bot.ModuleAuthor}/{bot.ModuleVersion} to modules list", Enums.LogLevels.Trace);
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

						foreach ((long uniqueId, IYoutubeClient plugin) in YoutubeModules) {
							try {
								Logger.Log($"Loading {plugin.ModuleIdentifier} module.", Enums.LogLevels.Trace);

								if (!IsExisitingModule(Enums.ModuleLoaderContext.YoutubeClients, plugin)) {
									if (plugin.InitModuleService()) {
										LoadedModules.YoutubeClients.Add((uniqueId, plugin));
										Logger.Log($"Loaded {plugin.ModuleIdentifier} module by {plugin.ModuleAuthor} | v{plugin.ModuleVersion}!", Enums.LogLevels.Trace);
									}
								}
								else {
									Logger.Log($"Not loading {uniqueId}/{plugin.ModuleAuthor}/{plugin.ModuleVersion} module as it already exists!", Enums.LogLevels.Trace);
								}
							}
							catch (Exception e) {
								Logger.Log(e);
							}
						}

						return true;
					}

				case Enums.ModulesContext.Steam: {
						HashSet<(long, ISteamClient)> SteamModules = new HashSet<(long, ISteamClient)>();
						try {
							using (CompositionHost container = configuration.CreateContainer()) {
								HashSet<ISteamClient> hashSet = container.GetExports<ISteamClient>().ToHashSet();

								if (hashSet.Count > 0) {
									foreach (ISteamClient bot in hashSet) {
										bot.ModuleIdentifier = GenerateModuleIdentifier();
										SteamModules.Add((bot.ModuleIdentifier, bot));
										Logger.Log($"Added bot {bot.ModuleIdentifier}/{bot.ModuleAuthor}/{bot.ModuleVersion} to modules list", Enums.LogLevels.Trace);
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

						foreach ((long uniqueId, ISteamClient plugin) in SteamModules) {
							try {
								Logger.Log($"Loading {plugin.ModuleIdentifier} module.", Enums.LogLevels.Trace);

								if (!IsExisitingModule(Enums.ModuleLoaderContext.SteamClients, plugin)) {
									if (plugin.InitModuleService()) {
										LoadedModules.SteamClients.Add((uniqueId, plugin));
										Logger.Log($"Loaded {plugin.ModuleIdentifier} module by {plugin.ModuleAuthor} | v{plugin.ModuleVersion}!", Enums.LogLevels.Trace);
									}
								}
								else {
									Logger.Log($"Not loading {uniqueId}/{plugin.ModuleAuthor}/{plugin.ModuleVersion} module as it already exists!", Enums.LogLevels.Trace);
								}
							}
							catch (Exception e) {
								Logger.Log(e);
							}
						}

						return true;
					}

				case Enums.ModulesContext.Misc: {
						HashSet<(long, IMiscModule)> MiscModules = new HashSet<(long, IMiscModule)>();
						try {
							using (CompositionHost container = configuration.CreateContainer()) {
								HashSet<IMiscModule> hashSet = container.GetExports<IMiscModule>().ToHashSet();

								if (hashSet.Count > 0) {
									foreach (IMiscModule bot in hashSet) {
										bot.ModuleIdentifier = GenerateModuleIdentifier();
										MiscModules.Add((bot.ModuleIdentifier, bot));
										Logger.Log($"Added bot {bot.ModuleIdentifier}/{bot.ModuleAuthor}/{bot.ModuleVersion} to modules list", Enums.LogLevels.Trace);
									}
								}
							}
						}
						catch (Exception e) {
							Logger.Log(e);
							return false;
						}

						if (MiscModules.Count <= 0) {
							return false;
						}

						foreach ((long uniqueId, IMiscModule plugin) in MiscModules) {
							try {
								Logger.Log($"Loading {plugin.ModuleIdentifier} module.", Enums.LogLevels.Trace);

								if (!IsExisitingModule(Enums.ModuleLoaderContext.MiscModules, plugin)) {
									if (plugin.InitModuleService()) {
										LoadedModules.MiscModules.Add((uniqueId, plugin));
										Logger.Log($"Loaded {plugin.ModuleIdentifier} module by {plugin.ModuleAuthor} | v{plugin.ModuleVersion}!", Enums.LogLevels.Trace);
									}
								}
								else {
									Logger.Log($"Not loading {uniqueId}/{plugin.ModuleAuthor}/{plugin.ModuleVersion} module as it already exists!", Enums.LogLevels.Trace);
								}
							}
							catch (Exception e) {
								Logger.Log(e);
							}
						}

						return true;
					}

				default:
					Logger.Log($"Unknown type of plugin loaded. cannot integret with {Core.AssistantName}.", Enums.LogLevels.Warn);
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

		public async Task<bool> OnCoreShutdown() {
			if (LoadedModules.IsModulesEmpty) {
				return true;
			}

			if (LoadedModules.DiscordBots.Count > 0) {
				foreach ((long UniqueId, IDiscordBot Dbot) in LoadedModules.DiscordBots) {
					Dbot.InitModuleShutdown();
				}
			}

			await Task.Delay(10).ConfigureAwait(false);

			if (LoadedModules.EmailClients.Count > 0) {
				foreach ((long UniqueId, IEmailClient Mbot) in LoadedModules.EmailClients) {
					Mbot.InitModuleShutdown();
				}
			}

			if (LoadedModules.GoogleMapModules.Count > 0) {
				foreach ((long UniqueId, IGoogleMap map) in LoadedModules.GoogleMapModules) {
					map.InitModuleShutdown();
				}
			}

			if (LoadedModules.YoutubeClients.Count > 0) {
				foreach ((long UniqueId, IYoutubeClient youtube) in LoadedModules.YoutubeClients) {
					youtube.InitModuleShutdown();
				}
			}

			if (LoadedModules.SteamClients.Count > 0) {
				foreach ((long UniqueId, ISteamClient steam) in LoadedModules.SteamClients) {
					steam.InitModuleShutdown();
				}
			}

			if (LoadedModules.MiscModules.Count > 0) {
				foreach ((long UniqueId, IMiscModule misc) in LoadedModules.MiscModules) {
					misc.InitModuleShutdown();
				}
			}

			Logger.Log("Module shutdown successfull.", Enums.LogLevels.Trace);
			return true;
		}

		private long GenerateModuleIdentifier() {
			byte[] buffer = new byte[8];
			Random.NextBytes(buffer);
			long longRand = BitConverter.ToInt64(buffer, 0);

			return Math.Abs(longRand % (100000000000000000 - 100000000000000050)) + 100000000000000050;
		}
	}
}
