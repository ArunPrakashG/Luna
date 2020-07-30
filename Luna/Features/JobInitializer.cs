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
		private readonly ObservableCollection<InternalJob> ObservableJobCollection = new ObservableCollection<InternalJob>();

		internal int JobCount => ObservableJobCollection.Count;

		internal InternalJob this[int index] {
			get => ObservableJobCollection[index] ?? throw new ArgumentOutOfRangeException(nameof(index));
			set => ObservableJobCollection[index] = value ?? throw new NullReferenceException(nameof(value));
		}

		internal JobInitializer() {
			ObservableJobCollection.CollectionChanged += OnJobCollectionChanged;
		}

		private void OnJobCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if(e == null || e.Action != NotifyCollectionChangedAction.Add) {
				return;
			}

			foreach(InternalJob? job in e.NewItems) {
				if(job == null) {
					continue;
				}

				OnJobLoaded(job);
			}
		}

		internal bool LoadInternalJobs() {
			try {
				ConventionBuilder conventions = new ConventionBuilder();
				conventions.ForTypesDerivedFrom<InternalJob>().Export<InternalJob>();
				IEnumerable<Assembly> psuedoCollection = new HashSet<Assembly>() { Assembly.GetExecutingAssembly() };
				ContainerConfiguration configuration = new ContainerConfiguration().WithAssemblies(psuedoCollection, conventions);
				using CompositionHost container = configuration.CreateContainer();
				List<InternalJob> jobsCollection = container.GetExports<InternalJob>().ToList();

				if (jobsCollection.Count <= 0) {
					Logger.Trace("No jobs exist to load.");
					return false;
				}

				for (int i = 0; i < jobsCollection.Count; i++) {
					Add(jobsCollection[i]);					
				}

				Logger.Info("Internal jobs loaded.");
				return true;
			}
			catch (Exception e) {
				Logger.Exception(e);
				return false;
			}
		}

		private void OnJobLoaded(InternalJob job) {
			if (job.HasJobExpired) {
				Logger.Warn($"'{job.JobName}' job has already expired.");
				Remove(job.UniqueID);
				return;
			}
						
			Logger.Info($"'{job.JobName}' job loaded.");
		}

		internal InternalJob GetJob(string uniqueId) {
			if (string.IsNullOrEmpty(uniqueId)) {
				return null;
			}

			return ObservableJobCollection.Where(x => x.UniqueID.Equals(uniqueId)).FirstOrDefault();
		}

		private bool IsExistingJob(string uniqueId) {
			if (string.IsNullOrEmpty(uniqueId) || ObservableJobCollection.Count <= 0) {
				return false;
			}

			return ObservableJobCollection.Where(x => x.UniqueID.Equals(uniqueId)).Count() >= 1;
		}

		internal void Remove(string uniqueID) {
			if (string.IsNullOrEmpty(uniqueID)) {
				return;
			}

			var job = ObservableJobCollection.Where(x => x.UniqueID.Equals(uniqueID, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

			if(job == null) {
				return;
			}

			job.Dispose();			
			ObservableJobCollection.Remove(job);
			Logger.Trace($"Disposed and removed job -> {job.UniqueID}");
		}

		internal void Add(InternalJob item) {
			if (item == null) {
				return;
			}

			if (IsExistingJob(item.UniqueID) || item.HasJobExpired) {
				return;
			}

			ObservableJobCollection.Add(item);
			Logger.Trace($"Added job -> {item.UniqueID}");
		}
	}
}
