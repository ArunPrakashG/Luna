using System;

namespace Luna.Features {
	internal interface IScheduledInternalJob {
		/// <summary>
		/// The <see cref="DateTime"/> at which the job is scheduled to fire initially.
		/// </summary>
		DateTime ScheduledAt { get; set; }

		/// <summary>
		/// Stores all the events associated with this job.
		/// </summary>
		JobEvents Events { get; set; }

		/// <summary>
		/// True will enable recurring of this job based on <see cref="DelayBetweenCalls"/> span.
		/// </summary>
		bool IsRecurring { get; set; }

		/// <summary>
		/// The delay between each fire of the event if the job is set to recurre.
		/// </summary>
		TimeSpan DelayBetweenCalls { get; set; }

		/// <summary>
		/// The job name.
		/// </summary>
		string JobName { get; set; }

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
	}
}
