using Luna.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;

namespace Luna.Features {
	internal class JobInitializer : ICollection<IScheduledInternalJob> {
		private readonly InternalLogger Logger = new InternalLogger(nameof(JobInitializer));
		private readonly List<IScheduledInternalJob> Jobs = new List<IScheduledInternalJob>();

		public int Count => Jobs.Count;

		public bool IsReadOnly => false;

		public IScheduledInternalJob this[int index] {
			get => Jobs[index] ?? throw new ArgumentOutOfRangeException(nameof(index));
			set => Jobs[index] = value ?? throw new NullReferenceException(nameof(value));
		}

		internal bool LoadInternalJobs() {
			try {
				ConventionBuilder conventions = new ConventionBuilder();
				conventions.ForTypesDerivedFrom<IScheduledInternalJob>().Export<IScheduledInternalJob>();
				IEnumerable<Assembly> psuedoCollection = new HashSet<Assembly>() { Assembly.GetExecutingAssembly() };
				ContainerConfiguration configuration = new ContainerConfiguration().WithAssemblies(psuedoCollection, conventions);
				using CompositionHost container = configuration.CreateContainer();
				List<IScheduledInternalJob> jobsCollection = container.GetExports<IScheduledInternalJob>().ToList();

				if (jobsCollection.Count <= 0) {
					Logger.Trace("No jobs exist to load.");
					return false;
				}

				for (int i = 0; i < jobsCollection.Count; i++) {
					IScheduledInternalJob job = jobsCollection[i];
					if (IsExistingJob(job.UniqueID)) {
						Logger.Info($"Skipping '{job.JobName} / {job.UniqueID}' job as it already exists.");
						continue;
					}

					OnJobLoaded(ref job);
				}

				Logger.Info("Internal jobs loaded.");
				return true;
			}
			catch (Exception e) {
				Logger.Exception(e);
				return false;
			}
		}

		private void OnJobLoaded(ref IScheduledInternalJob job) {
			if (job.HasJobExpired) {
				Logger.Warn($"'{job.JobName}' job has already expired.");
				return;
			}

			job.Events.OnJobInitialized?.Invoke();
			Jobs.Add(job);
			Logger.Info($"'{job.JobName}' job loaded.");
		}

		private bool IsExistingJob(string uniqueId) {
			if (string.IsNullOrEmpty(uniqueId) || Jobs.Count <= 0) {
				return false;
			}

			return Jobs.Where(x => x.UniqueID.Equals(uniqueId)).Count() == 1;
		}

		public void Add(IScheduledInternalJob item) {
			if (item == null) {
				return;
			}

			if (IsExistingJob(item.UniqueID)) {
				return;
			}

			Jobs.Add(item);
		}

		public void Clear() => Jobs.Clear();

		public bool Contains(IScheduledInternalJob item) => IsExistingJob(item.UniqueID);

		public void CopyTo(IScheduledInternalJob[] array, int arrayIndex) => Jobs.CopyTo(array, arrayIndex);

		public bool Remove(IScheduledInternalJob item) => Jobs.Remove(item);

		public IEnumerator<IScheduledInternalJob> GetEnumerator() => Jobs.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => Jobs.GetEnumerator();
	}
}
