using Assistant.Extensions;
using Assistant.Extensions.Interfaces;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using Assistant.Modules.Interfaces;
using Assistant.Modules.Interfaces.EventInterfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Modules {
	public class ModuleInitializer : IExternal {
		private readonly ILogger Logger = new Logger("MODULES");
		public readonly ObservableCollection<IModuleBase> Modules = new ObservableCollection<IModuleBase>();
		public HashSet<Assembly>? AssemblyCollection { get; private set; } = new HashSet<Assembly>();
		private static readonly SemaphoreSlim ModuleLoaderSemaphore = new SemaphoreSlim(1, 1);

		private readonly List<ModuleInfo<IModuleBase>> ModulesCache = new List<ModuleInfo<IModuleBase>>();

		private static ModuleInfo<T> GenerateDefault<T>() where T : IModuleBase => new ModuleInfo<T>(null, MODULE_TYPE.Unknown, false);

		public ModuleInitializer() {
			Modules.CollectionChanged += OnModuleAdded;
		}

		private void OnModuleAdded(object sender, NotifyCollectionChangedEventArgs e) {
			if (sender == null || e == null || e.NewItems.Count <= 0) {
				return;
			}

			foreach (var newItem in e.NewItems) {
				if (newItem == null) {
					continue;
				}

				IModuleBase? module = newItem as IModuleBase;

				if (module == null) {
					continue;
				}

				Helpers.InBackground(async () => await InitServiceOfTypeAsync<IModuleBase>(module).ConfigureAwait(false));
			}
		}

		public async Task<bool> LoadAsync() {
			AssemblyCollection?.Clear();
			AssemblyCollection = LoadAssemblies();

			if (AssemblyCollection == null || AssemblyCollection.Count <= 0) {
				Logger.Trace("No assemblies found.");
				return false;
			}

			await ModuleLoaderSemaphore.WaitAsync().ConfigureAwait(false);

			try {
				ConventionBuilder conventions = new ConventionBuilder();
				conventions.ForTypesDerivedFrom<IModuleBase>().Export<IModuleBase>();
				ContainerConfiguration configuration = new ContainerConfiguration().WithAssemblies(AssemblyCollection, conventions);

				using CompositionHost container = configuration.CreateContainer();
				List<IModuleBase> list = container.GetExports<IModuleBase>().ToList();

				if (list.Count > 0) {
					foreach (IModuleBase bot in list) {
						Logger.Trace($"Loading module of type {ParseModuleType(bot.ModuleType)} ...");
						bot.ModuleIdentifier = GenerateModuleIdentifier();
						ModuleInfo<IModuleBase> data = new ModuleInfo<IModuleBase>(bot.ModuleIdentifier, ParseModuleType(bot.ModuleType), true) {
							Module = bot
						};

						Logger.Trace($"Successfully loaded module with id {bot.ModuleIdentifier} of type {data.ModuleType.ToString()}");

						if (Modules.Count > 0 && !Modules.Contains(bot)) {
							Modules.Add(bot);
						}

						ModulesCache.Add(data);
					}
				}
			}
			catch (Exception e) {
				Logger.Exception(e);
				return false;
			}
			finally {
				ModuleLoaderSemaphore.Release();
			}

			return true;
		}

		private async Task<bool> InitServiceOfTypeAsync<TType>(TType module) where TType : IModuleBase {
			if (module == null) {
				return false;
			}

			Logger.Trace($"Starting {ParseModuleType(module.ModuleType).ToString()} module... ({module.ModuleIdentifier})");

			if (IsExisitingModule(module)) {
				return false;
			}

			if (module.RequiresInternetConnection && !Helpers.IsNetworkAvailable()) {
				return false;
			}

			await ModuleLoaderSemaphore.WaitAsync().ConfigureAwait(false);
			try {
				if (module.InitModuleService()) {
					Logger.Info($"Module loaded! {module.ModuleIdentifier}/{ParseModuleType(module.ModuleType).ToString()}");
					return true;
				}
			}
			finally {
				ModuleLoaderSemaphore.Release();
			}

			return false;
		}

		private static MODULE_TYPE ParseModuleType(int typeCode) {
			if (typeCode < 0 || typeCode > 4) {
				return MODULE_TYPE.Unknown;
			}

			return (MODULE_TYPE) typeCode;
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
					Logger.Trace($"Module of type {ParseModuleType(mod.ModuleType)} has been unloaded.");
					mod.IsLoaded = false;
					unloadedModules.Add(mod);
					continue;
				}
			}

			if (unloadedModules.Count > 0) {
				foreach (TType mod in unloadedModules) {
					if (Modules[Modules.IndexOf(mod)] != null) {
						Modules.RemoveAt(Modules.IndexOf(mod));
						Logger.Trace($"Module of type {ParseModuleType(mod.ModuleType)} has been removed from collection.");
					}
				}
			}

			return true;
		}

		public bool UnloadModuleWithId(string Id) {
			if (string.IsNullOrEmpty(Id) || Modules.Count <= 0) {
				return false;
			}

			IEnumerable<IModuleBase?> modules = Modules.Where(x => x.IsLoaded && x.ModuleIdentifier == Id);
			IModuleBase? module = modules.FirstOrDefault();

			if (modules == null || modules.Count() <= 0 || module == null) {
				return false;
			}

			return module.InitModuleShutdown();
		}
		
		public ModuleInfo<TType> FindModuleOfType<TType>(string identifier) where TType : IModuleBase {
			if (string.IsNullOrEmpty(identifier) || ModulesCache.Count <= 0 || !ModulesCache.OfType<TType>().Any()) {
				return GenerateDefault<TType>();
			}

			foreach (ModuleInfo<IModuleBase> mod in ModulesCache) {
				if (mod.ModuleIdentifier == identifier) {
					Logger.Trace($"Module found of type {mod.ModuleType} with identifier {mod.ModuleIdentifier}");

					ModuleInfo<TType> module = new ModuleInfo<TType>(mod.ModuleIdentifier, mod.ModuleType, mod.IsLoaded) {
						Module = (TType) mod.Module
					};

					return module;
				}
			}

			return GenerateDefault<TType>();
		}

		private bool IsExisitingModule<TType>(TType module) where TType : IModuleBase {
			if (module == null) {
				return false;
			}

			if (Modules.Count <= 0) {
				return false;
			}

			if (Modules.Any(x => x.ModuleIdentifier == module.ModuleIdentifier)) {
				Logger.Trace("This module is already loaded and added to module collection.");
				return true;
			}

			return false;
		}

		public bool UnloadFromPath(string assemblyPath) {
			if (string.IsNullOrEmpty(assemblyPath)) {
				return false;
			}

			if (Modules.Count <= 0) {
				return false;
			}

			assemblyPath = Path.GetFullPath(assemblyPath);
			IEnumerable<IModuleBase?> modules = Modules.Where(x => x.IsLoaded && x.ModulePath == assemblyPath);

			IModuleBase? module = modules.FirstOrDefault();

			if (modules == null || modules.Count() <= 0 || module == null) {
				return false;
			}

			return module.InitModuleShutdown();
		}

		private HashSet<Assembly>? LoadAssemblies() {
			HashSet<Assembly> assemblies = new HashSet<Assembly>();

			if (string.IsNullOrEmpty(Constants.HomeDirectory)) {
				return null;
			}

			string pluginsPath = Path.Combine(Constants.HomeDirectory, Constants.ModuleDirectory);

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
				return new HashSet<Assembly>();
			}

			return assemblies;
		}

		public void OnCoreShutdown() => UnloadModulesOfType<IModuleBase>();

		public async Task<bool> ExecuteAsyncEvent(MODULE_EXECUTION_CONTEXT context, object? fileEventSender = null, FileSystemEventArgs? fileEventArgs = null) {
			if (Modules.Count <= 0 || !Modules.OfType<IAsyncEventBase>().Any()) {
				return false;
			}

			foreach (IAsyncEventBase eventMod in Modules.OfType<IAsyncEventBase>()) {
				switch (context) {
					case MODULE_EXECUTION_CONTEXT.AssistantShutdown:
						if (!eventMod.IsLoaded) {
							continue;
						}

						return await eventMod.OnAssistantShutdownRequestedAsync().ConfigureAwait(false);

					case MODULE_EXECUTION_CONTEXT.AssistantStartup:
						return await eventMod.OnAssistantStartedAsync().ConfigureAwait(false);

					case MODULE_EXECUTION_CONTEXT.WatcherEvent:
						if (!eventMod.IsLoaded) {
							continue;
						}

						if (fileEventArgs == null || fileEventSender == null) {
							return false;
						}

						return await eventMod.OnWatcherEventRasiedAsync(fileEventSender, fileEventArgs).ConfigureAwait(false);

					case MODULE_EXECUTION_CONTEXT.NetworkDisconnected:
						if (!eventMod.IsLoaded) {
							continue;
						}

						return await eventMod.OnNetworkDisconnectedAsync().ConfigureAwait(false);

					case MODULE_EXECUTION_CONTEXT.NetworkReconnected:
						if (!eventMod.IsLoaded) {
							continue;
						}

						return await eventMod.OnNetworkReconnectedAsync().ConfigureAwait(false);

					case MODULE_EXECUTION_CONTEXT.SystemRestart:
						if (!eventMod.IsLoaded) {
							continue;
						}

						return await eventMod.OnSystemRestartRequestedAsync().ConfigureAwait(false);

					case MODULE_EXECUTION_CONTEXT.SystemShutdown:
						if (!eventMod.IsLoaded) {
							continue;
						}

						return await eventMod.OnSystemShutdownRequestedAsync().ConfigureAwait(false);

					case MODULE_EXECUTION_CONTEXT.UpdateAvailable:
						if (!eventMod.IsLoaded) {
							continue;
						}

						return await eventMod.OnUpdateAvailableAsync().ConfigureAwait(false);

					case MODULE_EXECUTION_CONTEXT.UpdateStarted:
						if (!eventMod.IsLoaded) {
							continue;
						}

						return await eventMod.OnUpdateStartedAsync().ConfigureAwait(false);

					default:
						return false;
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

		public void RegisterLoggerEvent(object? eventHandler) => LoggerExtensions.RegisterLoggerEvent(eventHandler);

		public enum MODULE_EXECUTION_CONTEXT {
			AssistantStartup,
			AssistantShutdown,
			UpdateAvailable,
			UpdateStarted,
			NetworkDisconnected,
			NetworkReconnected,
			SystemShutdown,
			SystemRestart,
			WatcherEvent
		}

		public enum MODULE_TYPE {
			Discord,
			Email,
			Steam,
			Youtube,
			Events,
			Unknown
		}
	}
}
