using Luna.Logging;
using Synergy.Extensions;
using System;
using System.Collections.Generic;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.Reflection;
using System.Threading.Tasks;

namespace Luna.TypeLoader {
	internal static class InternalTypeLoader {
		private static readonly InternalLogger Logger = new InternalLogger(nameof(InternalTypeLoader));

		internal static async IAsyncEnumerable<T> LoadInternalTypes<T>() where T : ILoadable {
			ConventionBuilder conventions = new ConventionBuilder();
			conventions.ForTypesDerivedFrom<T>().Export<T>();
			IEnumerable<Assembly> psuedoCollection = new HashSet<Assembly>() {
					Assembly.GetExecutingAssembly()
			};

			ContainerConfiguration configuration = new ContainerConfiguration().WithAssemblies(psuedoCollection, conventions);
			using (CompositionHost container = configuration.CreateContainer()) {
				var tasks = new List<Task>();

				foreach (var export in container.GetExports<T>()) {
					tasks.Add(new Task(() => export.OnLoaded()));
					yield return export;
				}

				await Helpers.InParallel(tasks).ConfigureAwait(false);
			}
		}
	}
}
