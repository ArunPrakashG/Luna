using Luna.Logging;
using Synergy.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;

namespace Luna.Features {
	internal class JobInitializer {
		private readonly InternalLogger Logger = new InternalLogger(nameof(JobInitializer));
		private readonly ObservableCollection<InternalJobBase> Jobs = new ObservableCollection<InternalJobBase>();

		internal int JobCount => Jobs.Count;

		public InternalJobBase this[int index] {
			get => Jobs[index] ?? throw new ArgumentOutOfRangeException(nameof(index));
			set => Jobs[index] = value ?? throw new NullReferenceException(nameof(value));
		}

		internal JobInitializer() {
			Jobs.CollectionChanged += OnJobCollectionChanged;
		}

		private void OnJobCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if(e == null || e.Action != NotifyCollectionChangedAction.Add) {

			}
		}

		internal bool LoadInternalJobs() {
			try {
				ConventionBuilder conventions = new ConventionBuilder();
				conventions.ForTypesDerivedFrom<InternalJobBase>().Export<InternalJobBase>();
				IEnumerable<Assembly> psuedoCollection = new HashSet<Assembly>() { Assembly.GetExecutingAssembly() };
				ContainerConfiguration configuration = new ContainerConfiguration().WithAssemblies(psuedoCollection, conventions);
				using CompositionHost container = configuration.CreateContainer();
				List<InternalJobBase> jobsCollection = container.GetExports<InternalJobBase>().ToList();

				if (jobsCollection.Count <= 0) {
					Logger.Trace("No jobs exist to load.");
					return false;
				}

				for (int i = 0; i < jobsCollection.Count; i++) {
					InternalJobBase job = jobsCollection[i];

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

		private void OnJobLoaded(ref InternalJobBase job) {
			if (job.HasJobExpired) {
				Logger.Warn($"'{job.JobName}' job has already expired.");
				return;
			}

			Jobs.Add(job);
			Logger.Info($"'{job.JobName}' job loaded.");
		}

		private bool IsExistingJob(string uniqueId) {
			if (string.IsNullOrEmpty(uniqueId) || Jobs.Count <= 0) {
				return false;
			}

			return Jobs.Where(x => x.UniqueID.Equals(uniqueId)).Count() == 1;
		}

		internal void Add(InternalJobBase item) {
			if (item == null) {
				return;
			}

			if (IsExistingJob(item.UniqueID)) {
				return;
			}

			Jobs.Add(item);
		}

		internal bool Contains(InternalJobBase item) => IsExistingJob(item.UniqueID);

		internal bool Remove(InternalJobBase item) => Jobs.Remove(item);
	}
}
