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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Modules {
	public class ModuleInitializer : IExternal{
		private readonly ILogger Logger = new Logger(typeof(ModuleInitializer).Name);		
		private HashSet<Assembly>? AssemblyCollection = new HashSet<Assembly>();
		private static readonly SemaphoreSlim ModuleLoaderSemaphore = new SemaphoreSlim(1, 1);
		private readonly List<ModuleInfo<IModuleBase>> ModulesCache = new List<ModuleInfo<IModuleBase>>();

		public static readonly ObservableCollection<IModuleBase> Modules = new ObservableCollection<IModuleBase>();
		private static ModuleInfo<IModuleBase> GenerateDefault() => new ModuleInfo<IModuleBase>(null, default, false);

		public ModuleInitializer() => Modules.CollectionChanged += OnModuleAdded;

		private void OnModuleAdded(object sender, NotifyCollectionChangedEventArgs e) {
			if (sender == null || e == null || e.NewItems == null || e.NewItems.Count <= 0) {
				return;
			}
			
			foreach (IModuleBase module in e.NewItems) {
				if (module == null) {
					continue;
				}

				Helpers.InBackground(async () => await InitServiceOfTypeAsync(module).ConfigureAwait(false));
			}
		}

		public async Task<bool> LoadAsync<T>() where T: IModuleBase {
			AssemblyCollection?.Clear();
			AssemblyCollection = LoadAssemblies();

			if (AssemblyCollection == null || AssemblyCollection.Count <= 0) {
				Logger.Trace("No assemblies found.");
				return false;
			}

			await ModuleLoaderSemaphore.WaitAsync().ConfigureAwait(false);

			try {
				ConventionBuilder conventions = new ConventionBuilder();
				conventions.ForTypesDerivedFrom<T>().Export<T>();
				ContainerConfiguration configuration = new ContainerConfiguration().WithAssemblies(AssemblyCollection, conventions);

				using CompositionHost container = configuration.CreateContainer();
				List<IModuleBase> list = container.GetExports<IModuleBase>().ToList();

				if (list.Count > 0) {
					foreach (IModuleBase bot in list) {
						Logger.Trace($"Loading module {bot.ModuleIdentifier} ...");
						bot.ModuleIdentifier = GenerateModuleIdentifier();
						ModuleInfo<IModuleBase> data = new ModuleInfo<IModuleBase>(bot.ModuleIdentifier, bot, true);
						Logger.Trace($"Successfully loaded module with id {bot.ModuleIdentifier} !");

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

		private async Task<bool> InitServiceOfTypeAsync<T>(T module) where T: IModuleBase {
			if (module == null) {
				return false;
			}

			Logger.Trace($"Starting module... ({module.ModuleIdentifier})");

			if (IsExisitingModule(module)) {
				return false;
			}

			if (module.RequiresInternetConnection && !Helpers.IsNetworkAvailable()) {
				return false;
			}

			await ModuleLoaderSemaphore.WaitAsync().ConfigureAwait(false);
			try {
				if (module.InitModuleService()) {
					Logger.Info($"Module loaded! {module.ModuleIdentifier}");
					return true;
				}
			}
			finally {
				ModuleLoaderSemaphore.Release();
			}

			return false;
		}

		public bool UnloadModulesOfType() {
			if (Modules.Count <= 0) {
				return false;
			}

			if (!Modules.OfType<IModuleBase>().Any()) {
				return false;
			}

			List<IModuleBase> unloadedModules = new List<IModuleBase>();
			foreach (IModuleBase mod in Modules.OfType<IModuleBase>()) {
				if (mod.IsLoaded && mod.InitModuleShutdown()) {
					Logger.Trace($"Module has been unloaded.");
					mod.IsLoaded = false;
					unloadedModules.Add(mod);
					continue;
				}
			}

			if (unloadedModules.Count > 0) {
				foreach (IModuleBase mod in unloadedModules) {
					if (Modules[Modules.IndexOf(mod)] != null) {
						Modules.RemoveAt(Modules.IndexOf(mod));
						Logger.Trace($"Module has been removed from collection.");
					}
				}
			}

			return true;
		}

		public bool UnloadModuleWithId(string id) {
			if (string.IsNullOrEmpty(id) || Modules.Count <= 0) {
				return false;
			}

			IEnumerable<IModuleBase> modules = Modules.Where(x => x.IsLoaded && x.ModuleIdentifier == id);
			IModuleBase module = modules.FirstOrDefault();

			if (modules == null || modules.Count() <= 0 || module == null) {
				return false;
			}

			return module.InitModuleShutdown();
		}

		public ModuleInfo<IModuleBase> FindModuleOfType(string identifier) {
			if (string.IsNullOrEmpty(identifier) || ModulesCache.Count <= 0 || !ModulesCache.OfType<IModuleBase>().Any()) {
				return GenerateDefault();
			}

			foreach (ModuleInfo<IModuleBase> mod in ModulesCache.OfType<ModuleInfo<IModuleBase>>()) {
				if (mod.ModuleIdentifier == identifier) {
					Logger.Trace($"Module found with identifier {mod.ModuleIdentifier}");
					return mod;
				}
			}

			return GenerateDefault();
		}

		private bool IsExisitingModule(IModuleBase module) {
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
			IEnumerable<IModuleBase> modules = Modules.Where(x => x.IsLoaded && x.ModulePath == assemblyPath);
			IModuleBase module = modules.FirstOrDefault();

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

		public void OnCoreShutdown() => UnloadModulesOfType();

		public static EventResponse ExecuteAsyncEvent<E>(MODULE_EXECUTION_CONTEXT context, EventParameter parameters) where E : IEvent {
			if (Modules.Count <= 0 || !Modules.OfType<E>().Any()) {
				return new EventResponse("failed.", false);
			}

			foreach (E mod in Modules.OfType<E>()) {
				if (!mod.IsLoaded) {
					continue;
				}

				switch (context) {
					case MODULE_EXECUTION_CONTEXT.AssistantShutdown:
						return mod.OnAssistantShutdownRequestedAsync().Invoke(parameters);
					case MODULE_EXECUTION_CONTEXT.AssistantStartup:
						return mod.OnAssistantStartedAsync().Invoke(parameters);
					case MODULE_EXECUTION_CONTEXT.WatcherEvent:
						if (parameters.Values.Length < 2) {
							return new EventResponse("Parameters are invalid.", false);
						}

						return mod.OnWatcherEventRasiedAsync().Invoke(parameters);
					case MODULE_EXECUTION_CONTEXT.NetworkDisconnected:
						return mod.OnNetworkDisconnectedAsync().Invoke(parameters);
					case MODULE_EXECUTION_CONTEXT.NetworkReconnected:
						return mod.OnNetworkReconnectedAsync().Invoke(parameters);
					case MODULE_EXECUTION_CONTEXT.SystemRestart:
						return mod.OnSystemRestartRequestedAsync().Invoke(parameters);
					case MODULE_EXECUTION_CONTEXT.SystemShutdown:
						return mod.OnSystemShutdownRequestedAsync().Invoke(parameters);
					case MODULE_EXECUTION_CONTEXT.UpdateAvailable:
						return mod.OnUpdateAvailableAsync().Invoke(parameters);
					case MODULE_EXECUTION_CONTEXT.UpdateStarted:
						return mod.OnUpdateStartedAsync().Invoke(parameters);
				}
			}

			return new EventResponse("failed.", false);
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
	}
}
