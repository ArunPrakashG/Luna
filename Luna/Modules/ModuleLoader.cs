using Luna.Logging;
using Luna.Modules.Interfaces;
using Synergy.Extensions;
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

namespace Luna.Modules {
	public sealed class ModuleLoader {
		private static readonly SemaphoreSlim ModuleLoaderSemaphore = new SemaphoreSlim(1, 1);
		private readonly InternalLogger Logger = new InternalLogger(nameof(ModuleLoader));
		private readonly List<ModuleWrapper<IModule>> ModulesCache;
		internal static readonly ObservableCollection<IModule> Modules;

		static ModuleLoader() {
			Modules = new ObservableCollection<IModule>();
		}

		internal ModuleLoader() {
			ModulesCache = new List<ModuleWrapper<IModule>>();
			Modules.CollectionChanged += OnModuleAdded;
		}

		private void OnModuleAdded(object sender, NotifyCollectionChangedEventArgs e) {
			if (sender == null || e == null || e.NewItems == null || e.NewItems.Count <= 0) {
				return;
			}

			foreach (IModule? module in e.NewItems) {
				if (module == null) {
					continue;
				}

				Helpers.InBackground(() => InitServiceOfTypeAsync(module));
			}
		}

		internal async Task<bool> LoadAsync(bool isEnabled) {
			if (!isEnabled) {
				return false;
			}

			var assemblyCollection = LoadAssemblies();

			if (assemblyCollection == null || assemblyCollection.Count <= 0) {
				Logger.Trace("No assemblies found.");
				return false;
			}

			assemblyCollection.Add(Assembly.GetExecutingAssembly());
			await ModuleLoaderSemaphore.WaitAsync().ConfigureAwait(false);

			try {
				ConventionBuilder conventions = new ConventionBuilder();
				conventions.ForTypesDerivedFrom<IModule>().Export<IModule>();
				ContainerConfiguration configuration = new ContainerConfiguration().WithAssemblies(assemblyCollection, conventions);

				using CompositionHost container = configuration.CreateContainer();

				foreach (IModule module in container.GetExports<IModule>()) {
					Logger.Trace($"Loading module {module.ModuleIdentifier} ...");
					module.ModuleIdentifier = GenerateModuleIdentifier(module);
					ModuleWrapper<IModule> data = new ModuleWrapper<IModule>(module.ModuleIdentifier, module, true);
					Logger.Trace($"Loaded module with id {module.ModuleIdentifier} !");

					if (!IsExisitingModule(module)) {
						Modules.Add(module);
					}

					ModulesCache.Add(data);
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

		private void InitServiceOfTypeAsync<T>(T module) where T : IModule {
			if (module == null) {
				return;
			}

			Logger.Trace($"Starting module... ({module.ModuleIdentifier})");

			if (IsExisitingModule(module)) {
				return;
			}

			if (module.RequiresInternetConnection && !Helpers.IsNetworkAvailable()) {
				return;
			}

			if (module.InitModuleService()) {
				Logger.Info($"Module loaded! ({module.ModuleIdentifier})");
				return;
			}
		}

		private void UnloadModulesOfType() {
			if (Modules.Count <= 0) {
				return;
			}

			if (!Modules.OfType<IModule>().Any()) {
				return;
			}

			List<IModule> unloadedModules = new List<IModule>();
			foreach (IModule mod in Modules.OfType<IModule>()) {
				if (mod.IsLoaded && mod.InitModuleShutdown()) {
					Logger.Trace($"Module has been unloaded.");
					mod.IsLoaded = false;
					unloadedModules.Add(mod);
					continue;
				}
			}

			if (unloadedModules.Count > 0) {
				foreach (IModule mod in unloadedModules) {
					if (Modules[Modules.IndexOf(mod)] != null) {
						Modules.RemoveAt(Modules.IndexOf(mod));
						Logger.Trace($"Module has been removed from collection.");
					}
				}
			}
		}

		private bool UnloadModuleWithId(string id) {
			if (string.IsNullOrEmpty(id) || Modules.Count <= 0) {
				return false;
			}

			IEnumerable<IModule> modules = Modules.Where(x => x.IsLoaded && x.ModuleIdentifier == id);
			IModule module = modules.FirstOrDefault();

			if (modules == null || modules.Count() <= 0 || module == null) {
				return false;
			}

			return module.InitModuleShutdown();
		}

		private ModuleWrapper<IModule> FindModuleOfType(string identifier) {
			if (string.IsNullOrEmpty(identifier) || ModulesCache.Count <= 0 || !ModulesCache.OfType<IModule>().Any()) {
				return default;
			}

			foreach (ModuleWrapper<IModule> mod in ModulesCache.OfType<ModuleWrapper<IModule>>()) {
				if (mod.ModuleIdentifier == identifier) {
					Logger.Trace($"Module found with identifier {mod.ModuleIdentifier}");
					return mod;
				}
			}

			return default;
		}

		private bool IsExisitingModule(IModule module) {
			if (module == null) {
				return false;
			}

			if (Modules.Count <= 0) {
				return false;
			}

			if (Modules.Any(x => x.ModuleIdentifier == module.ModuleIdentifier)) {
				return true;
			}

			return false;
		}

		internal bool UnloadFromPath(string assemblyPath) {
			if (string.IsNullOrEmpty(assemblyPath) || Modules.Count <= 0) {
				return false;
			}

			assemblyPath = Path.GetFullPath(assemblyPath);
			IEnumerable<IModule> modules = Modules.Where(x => x.IsLoaded && x.ModulePath == assemblyPath);
			IModule module = modules.FirstOrDefault();

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

		internal void OnCoreShutdown() => UnloadModulesOfType();

		public static void ExecuteActionOnType<T>(Action<T> eventAction) {
			if (Modules.Count <= 0) {
				return;
			}

			foreach (T module in Modules.OfType<T>()) {
				eventAction.Invoke(module);
			}
		}

		private string GenerateModuleIdentifier(IModule module) => string.Format("{0}/{1}/{2}", module.ModuleType.ToString(), new Guid().ToString("N"), DateTime.Now.Ticks.ToString());
	}
}
