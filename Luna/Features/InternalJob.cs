using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Luna.Features {
	internal abstract class InternalJob : IDisposable {
		/// <summary>
		/// The unique ID which is used to identify this job.
		/// </summary>
		internal readonly string UniqueID;

		/// <summary>
		/// The <see cref="DateTime"/> at which the job is scheduled to fire initially.
		/// </summary>
		internal readonly DateTime ScheduledAt;

		/// <summary>
		/// True will enable recurring of this job based on <see cref="DelayBetweenCalls"/> span.
		/// </summary>
		internal readonly bool IsRecurring;

		/// <summary>
		/// The delay between each fire of the event if the job is set to recurre.
		/// </summary>
		internal readonly TimeSpan DelayBetweenCalls;

		/// <summary>
		/// The job name.
		/// </summary>
		internal readonly string JobName;

		/// <summary>
		/// Optional object parameters to be passed on invoking <see cref="OnJobTriggered"/>.
		/// </summary>
		internal readonly ObjectParameterWrapper? JobParameters;

		/// <summary>
		/// If <see cref="ScheduledAt"/> time is less than <see cref="DateTime.Now"/> then the job is expired as its unable to fire.
		/// </summary>
		internal bool HasJobExpired => DateTime.Now > ScheduledAt;

		/// <summary>
		/// The <see cref="TimeSpan"/> untill initial call of this job.
		/// </summary>
		internal TimeSpan SpanUntilInitialCall => (DateTime.Now - ScheduledAt);

		private bool IsDisposed;
		private Timer JobTimer;		

		internal InternalJob(string jobName, DateTime scheduledAt, ObjectParameterWrapper? parameters = null) {
			JobName = jobName ?? throw new ArgumentNullException(nameof(jobName));
			ScheduledAt = scheduledAt.Equals(DateTime.MinValue) ? throw new ArgumentOutOfRangeException(nameof(scheduledAt)) : scheduledAt;
			UniqueID = JobName + "/" + new Guid().ToString("N") + "/" + ScheduledAt.Ticks;
			IsRecurring = false;
			DelayBetweenCalls = Timeout.InfiniteTimeSpan;
			JobParameters = parameters;
		}

		internal InternalJob(string jobName, DateTime scheduledAt, TimeSpan delayBetweenCalls, ObjectParameterWrapper? parameters = null) {
			JobName = jobName ?? throw new ArgumentNullException(nameof(jobName));
			ScheduledAt = scheduledAt.Equals(DateTime.MinValue) ? throw new ArgumentOutOfRangeException(nameof(scheduledAt)) : scheduledAt;
			UniqueID = JobName?.Replace("\n", "")?.Replace(" ", "") + "/" + new Guid().ToString("N") + "/" + ScheduledAt.Ticks;
			IsRecurring = delayBetweenCalls != Timeout.InfiniteTimeSpan;
			DelayBetweenCalls = IsRecurring ? delayBetweenCalls : Timeout.InfiniteTimeSpan;
			JobParameters = parameters;
		}

		/// <summary>
		/// Sets the internal job timer.
		/// </summary>
		/// <param name="timer"></param>
		internal void SetJobTimer(Timer timer) => JobTimer = timer;

		/// <summary>
		/// Fired when this job is loaded and ready to be fired at <see cref="ScheduledAt"/> time.
		/// </summary>
		internal abstract void OnJobInitialized();

		/// <summary>
		/// Fired when <see cref="ScheduledAt"/> time is reached.
		/// </summary>
		/// <param name="objectParameterWrapper">Optional object parameter wrapper.</param>
		internal abstract void OnJobTriggered(ObjectParameterWrapper? objectParameterWrapper);

		/// <summary>
		/// Fired when the job is loaded.
		/// </summary>
		internal abstract void OnJobLoaded();

		/// <summary>
		/// <inheritdoc />
		/// </summary>
		public void Dispose() {
			IsDisposed = true;
			JobTimer?.Dispose();
		}
	}
}
