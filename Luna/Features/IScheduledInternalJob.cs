using System;

namespace Luna.Features {
	internal interface IScheduledInternalJob {
		/// <summary>
		/// The <see cref="DateTime"/> at which the job is scheduled to fire initially.
		/// </summary>
		DateTime ScheduledAt { get; }

		/// <summary>
		/// True will enable recurring of this job based on <see cref="DelayBetweenCalls"/> span.
		/// </summary>
		bool IsRecurring { get; }

		/// <summary>
		/// The delay between each fire of the event if the job is set to recurre.
		/// </summary>
		TimeSpan DelayBetweenCalls { get; }

		/// <summary>
		/// The job name.
		/// </summary>
		string JobName { get; }

		/// <summary>
		/// The unique ID which is used to identify this job.
		/// </summary>
		string UniqueID => !string.IsNullOrEmpty(JobName) ? JobName.GetHashCode().ToString() : (Events.GetHashCode() + ScheduledAt.GetHashCode()).ToString();

		/// <summary>
		/// If the <see cref="ScheduledAt"/> time is less than <see cref="DateTime.Now"/> then the job is expired as its unable to fire.
		/// </summary>
		bool HasJobExpired => DateTime.Now > ScheduledAt;

		/// <summary>
		/// The <see cref="TimeSpan"/> untill initial call of this job.
		/// </summary>
		TimeSpan SpanUntilInitialCall => (DateTime.Now - ScheduledAt);

		void OnJobInitialized();

		void OnEventFired(ObjectParameterWrapper? objectParameterWrapper);

		void OnJobLoaded();
	}
}
