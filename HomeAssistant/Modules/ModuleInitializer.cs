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
using HomeAssistant.Core;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Modules {
	public class Modules {
		public HashSet<IEmailClient> EmailModules { get; set; } = new HashSet<IEmailClient>();
		public HashSet<IDiscordBot> DiscordModules { get; set; } = new HashSet<IDiscordBot>();
		public HashSet<IGoogleMap> GoogleMapModules { get; set; } = new HashSet<IGoogleMap>();
		public HashSet<ISteamClient> SteamModules { get; set; } = new HashSet<ISteamClient>();
		public HashSet<IYoutubeClient> YoutubeModules { get; set; } = new HashSet<IYoutubeClient>();
		public HashSet<ILoggerBase> LoggerBase { get; set; } = new HashSet<ILoggerBase>();
		public HashSet<IMiscModule> MiscModules { get; set; } = new HashSet<IMiscModule>();

		public HashSet<Assembly> LoadedAssemblies { get; set; } = new HashSet<Assembly>();

		public HashSet<IEmailClient> FailedEmailModules { get; set; } = new HashSet<IEmailClient>();
		public HashSet<IDiscordBot> FailedDiscordModules { get; set; } = new HashSet<IDiscordBot>();
		public HashSet<IGoogleMap> FailedGoogleMapModules { get; set; } = new HashSet<IGoogleMap>();
		public HashSet<ISteamClient> FailedSteamModules { get; set; } = new HashSet<ISteamClient>();
		public HashSet<IYoutubeClient> FailedYoutubeModules { get; set; } = new HashSet<IYoutubeClient>();
		public HashSet<ILoggerBase> FailedLoggerBases { get; set; } = new HashSet<ILoggerBase>();
		public HashSet<IMiscModule> FailedMiscModules { get; set; } = new HashSet<IMiscModule>();

		public bool IsModulesEmpty =>
			EmailModules.Count <= 0 && DiscordModules.Count <= 0 && GoogleMapModules.Count <= 0 &&
			SteamModules.Count <= 0 && YoutubeModules.Count <= 0 && LoggerBase.Count <= 0;

		public bool IsEmailModuleEmpty => EmailModules.Count <= 0;

		public bool IsDiscordModuleEmpty => DiscordModules.Count <= 0;

		public bool IsGoogleMapModuleEmpty => GoogleMapModules.Count <= 0;

		public bool IsSteamModuleEmpty => SteamModules.Count <= 0;

		public bool IsYoutubeModuleEmpty => YoutubeModules.Count <= 0;

		public bool IsLoggerBaseEmpty => LoggerBase.Count <= 0;
		public bool IsMiscModuleEmpty => MiscModules.Count <= 0;
	}

	public class ModuleInitializer {
		private readonly Logger Logger = new Logger("MODULES");
		public Modules Modules { get; set; } = new Modules();
		public List<IDiscordBot> Discord { get; private set; } = new List<IDiscordBot>();
		public List<IGoogleMap> Map { get; private set; } = new List<IGoogleMap>();
		public List<IYoutubeClient> Youtube { get; private set; } = new List<IYoutubeClient>();
		public List<IEmailClient> Mail { get; private set; } = new List<IEmailClient>();
		public List<ISteamClient> Steam { get; private set; } = new List<ISteamClient>();
		public List<IMiscModule> MiscModule { get; private set; } = new List<IMiscModule>();

		//public (Discord, Email, GoogleMap, Youtube) StartModules() {
		//	if (Tess.Config.DiscordBot) {
		//		Discord = new Discord();
		//		(bool, Discord) discordResult = Task.Run(async () => await Discord.RegisterDiscordClient().ConfigureAwait(false)).Result;
		//	}

		//	if (Tess.Config.EnableEmail) {
		//		Mail = new Email();
		//		(bool, ConcurrentDictionary<string, EmailBot>) emailResult = Mail.InitEmailBots();
		//	}

		//	if (Tess.Config.EnableGoogleMap) {
		//		Map = new GoogleMap();
		//	}

		//	if (Tess.Config.EnableYoutube) {
		//		Youtube = new Youtube();
		//	}

		//	return (Discord ?? null, Mail ?? null, Map ?? null, Youtube ?? null);
		//}

		[NotNull]
		public (bool, Modules) LoadModules() {
			if (!Modules.IsModulesEmpty) {
				return (false, Modules);
			}

			Modules.LoadedAssemblies = LoadAssemblies();

			if ((Modules.LoadedAssemblies == null) || (Modules.LoadedAssemblies.Count == 0)) {
				Logger.Log("No modules found.", LogLevels.Error);
				return (false, Modules);
			}

			Logger.Log("Loading modules...");
			if (Load<IEmailClient>(ModulesContext.Email)) {
				Logger.Log("All Email modules have been loaded.", LogLevels.Trace);
			}

			if (Load<IDiscordBot>(ModulesContext.Discord)) {
				Logger.Log("Discord bot has been loaded.", LogLevels.Trace);
			}

			if (Load<ISteamClient>(ModulesContext.Steam)) {
				Logger.Log("Steam client has been loaded.", LogLevels.Trace);
			}

			if (Load<IGoogleMap>(ModulesContext.GoogleMap)) {
				Logger.Log("Google map serivces has been loaded.", LogLevels.Trace);
			}

			if (Load<IYoutubeClient>(ModulesContext.Youtube)) {
				Logger.Log("Youtube client has been loaded.", LogLevels.Trace);
			}

			if (Load<IMiscModule>(ModulesContext.Misc)) {
				Logger.Log("Misc modules have been loaded.", LogLevels.Trace);
			}

			if (Modules.IsEmailModuleEmpty) {
				Tess.DisableEmailMethods = true;
			}

			if (Modules.IsDiscordModuleEmpty) {
				Tess.DisableDiscordMethods = true;
			}

			if (Modules.IsSteamModuleEmpty) {
				Tess.DisableSteamMethods = true;
			}

			return (true, Modules);
		}

		[NotNull]
		private bool Load<T>(ModulesContext context) where T : IModuleBase {
			ConventionBuilder conventions = new ConventionBuilder();
			conventions.ForTypesDerivedFrom<T>().Export<T>();
			ContainerConfiguration configuration = new ContainerConfiguration().WithAssemblies(Modules.LoadedAssemblies, conventions);

			switch (context) {
				case ModulesContext.Discord:
					try {
						using (CompositionHost container = configuration.CreateContainer()) {
							Modules.DiscordModules = container.GetExports<IDiscordBot>().ToHashSet();
						}
					}
					catch (Exception e) {
						Logger.Log(e);
						return false;
					}

					if (Modules.IsDiscordModuleEmpty) {
						return false;
					}

					Modules.FailedDiscordModules = new HashSet<IDiscordBot>();

					foreach (IDiscordBot plugin in Modules.DiscordModules) {
						try {
							Logger.Log($"Loading {plugin.ModuleIdentifier} module.", LogLevels.Trace);
							if (plugin.InitModuleService()) {
								Discord.Add(plugin);
							}

							Logger.Log($"Loaded {plugin.ModuleIdentifier} module by {plugin.ModuleAuthor} | v{plugin.ModuleVersion}!");
							return true;
						}
						catch (Exception e) {
							Logger.Log(e);
							Modules.FailedDiscordModules.Add(plugin);
						}
					}

					break;
				case ModulesContext.Email:
					try {
						using (CompositionHost container = configuration.CreateContainer()) {
							Modules.EmailModules = container.GetExports<IEmailClient>().ToHashSet();
						}
					}
					catch (Exception e) {
						Logger.Log(e);
						return false;
					}

					if (Modules.IsEmailModuleEmpty) {
						return false;
					}

					Modules.FailedEmailModules = new HashSet<IEmailClient>();

					foreach (IEmailClient plugin in Modules.EmailModules) {
						try {
							Logger.Log($"Loading {plugin.ModuleIdentifier} module.", LogLevels.Trace);
							if (plugin.InitModuleService()) {
								Mail.Add(plugin);
							}

							Logger.Log($"Loaded {plugin.ModuleIdentifier} module by {plugin.ModuleAuthor} | v{plugin.ModuleVersion}!");
							return true;
						}
						catch (Exception e) {
							Logger.Log(e);
							Modules.FailedEmailModules.Add(plugin);
						}
					}

					break;

				case ModulesContext.GoogleMap:
					try {
						using (CompositionHost container = configuration.CreateContainer()) {
							Modules.GoogleMapModules = container.GetExports<IGoogleMap>().ToHashSet();
						}
					}
					catch (Exception e) {
						Logger.Log(e);
						return false;
					}

					if (Modules.IsGoogleMapModuleEmpty) {
						return false;
					}

					Modules.FailedGoogleMapModules = new HashSet<IGoogleMap>();

					foreach (IGoogleMap plugin in Modules.GoogleMapModules) {
						try {
							Logger.Log($"Loading {plugin.ModuleIdentifier} module.", LogLevels.Trace);

							if (plugin.InitModuleService()) {
								Map.Add(plugin);
							}

							Logger.Log($"Loaded {plugin.ModuleIdentifier} module by {plugin.ModuleAuthor} | v{plugin.ModuleVersion}!");
							return true;
						}
						catch (Exception e) {
							Logger.Log(e);
							Modules.FailedGoogleMapModules.Add(plugin);
						}
					}

					break;

				case ModulesContext.Youtube:
					try {
						using (CompositionHost container = configuration.CreateContainer()) {
							Modules.YoutubeModules = container.GetExports<IYoutubeClient>().ToHashSet();
						}
					}
					catch (Exception e) {
						Logger.Log(e);
						return false;
					}

					if (Modules.IsYoutubeModuleEmpty) {
						return false;
					}

					Modules.FailedYoutubeModules = new HashSet<IYoutubeClient>();

					foreach (IYoutubeClient plugin in Modules.YoutubeModules) {
						try {
							Logger.Log($"Loading {plugin.ModuleIdentifier} module.", LogLevels.Trace);

							if (plugin.InitModuleService()) {
								Youtube.Add(plugin);
							}

							Logger.Log($"Loaded {plugin.ModuleIdentifier} module by {plugin.ModuleAuthor} | v{plugin.ModuleVersion}!");
							return true;
						}
						catch (Exception e) {
							Logger.Log(e);
							Modules.FailedYoutubeModules.Add(plugin);
						}
					}

					break;
				case ModulesContext.Steam:
					try {
						using (CompositionHost container = configuration.CreateContainer()) {
							Modules.SteamModules = container.GetExports<ISteamClient>().ToHashSet();
						}
					}
					catch (Exception e) {
						Logger.Log(e);
						return false;
					}

					if (Modules.IsSteamModuleEmpty) {
						return false;
					}

					Modules.FailedSteamModules = new HashSet<ISteamClient>();

					foreach (ISteamClient plugin in Modules.SteamModules) {
						try {
							Logger.Log($"Loading {plugin.ModuleIdentifier} module.", LogLevels.Trace);

							if (plugin.InitModuleService()) {
								Steam.Add(plugin);
							}

							Logger.Log($"Loaded {plugin.ModuleIdentifier} module by {plugin.ModuleAuthor} | v{plugin.ModuleVersion}!");
							return true;
						}
						catch (Exception e) {
							Logger.Log(e);
							Modules.FailedSteamModules.Add(plugin);
						}
					}

					break;
				case ModulesContext.Misc:
					try {
						using (CompositionHost container = configuration.CreateContainer()) {
							Modules.MiscModules = container.GetExports<IMiscModule>().ToHashSet();
						}
					}
					catch (Exception e) {
						Logger.Log(e);
						return false;
					}

					if (Modules.IsMiscModuleEmpty) {
						return false;
					}

					Modules.FailedMiscModules = new HashSet<IMiscModule>();

					foreach (IMiscModule plugin in Modules.MiscModules) {
						try {
							Logger.Log($"Loading {plugin.ModuleIdentifier} module.", LogLevels.Trace);

							if (plugin.InitModuleService()) {
								MiscModule.Add(plugin);
							}

							Logger.Log($"Loaded {plugin.ModuleIdentifier} module by {plugin.ModuleAuthor} | v{plugin.ModuleVersion}!");
							return true;
						}
						catch (Exception e) {
							Logger.Log(e);
							Modules.FailedMiscModules.Add(plugin);
						}
					}

					break;
				default:
					Logger.Log("Unknown type of plugin loaded. cannot integret with tess.", LogLevels.Warn);
					return false;
			}
			return false;
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
						Logger.Log($"Assembly path is invalid. {assemblyPath}");
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
			if (Discord.Count > 0) {
				foreach (IDiscordBot Dbot in Discord) {
					if (Dbot.IsServerOnline) {
						Logger.Log("Discord server shutting down...", LogLevels.Trace);
						Dbot.InitModuleShutdown();
					}
				}
			}

			await Task.Delay(10).ConfigureAwait(false);
			if (Mail.Count > 0) {
				foreach (IEmailClient Mbot in Mail) {
					if (Mbot.EmailClientCollection.Count > 0) {
						Mbot.InitModuleShutdown();
					}
				}
			}

			Logger.Log("Module shutdown successfull.", LogLevels.Trace);
			return true;
		}
	}
}
