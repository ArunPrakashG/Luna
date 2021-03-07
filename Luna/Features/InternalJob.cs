using Synergy.Extensions;
using System;
using System.Threading;

namespace Luna.Features {
	/// <summary>
	/// Provides an interface to schedule jobs
	/// </summary>
	public abstract class InternalJob : IDisposable {
		/// <summary>
		/// The unique ID which is used to identify this job.
		/// </summary>
		public readonly string UniqueID;

		/// <summary>
		/// The <see cref="DateTime"/> at which the job is scheduled to fire initially.
		/// </summary>
		public readonly DateTime ScheduledAt;

		/// <summary>
		/// True will enable recurring of this job based on <see cref="DelayBetweenCalls"/> span.
		/// </summary>
		public bool IsRecurring => DelayBetweenCalls != Timeout.InfiniteTimeSpan;

		/// <summary>
		/// The delay between each fire of the event if the job is set to recurre.
		/// </summary>
		public TimeSpan DelayBetweenCalls { get; private set; }

		/// <summary>
		/// The job name.
		/// </summary>
		public readonly string JobName;

		/// <summary>
		/// Optional object parameters to be passed on invoking <see cref="OnJobTriggered"/>.
		/// </summary>
		public readonly ObjectParameterWrapper? JobParameters;

		/// <summary>
		/// If <see cref="ScheduledAt"/> time is less than <see cref="DateTime.Now"/> then the job is expired as its unable to fire.
		/// </summary>
		public bool HasJobExpired => DateTime.Now > ScheduledAt;

		/// <summary>
		/// The <see cref="TimeSpan"/> untill initial call of this job.
		/// </summary>
		public TimeSpan SpanUntilInitialCall => (DateTime.Now - ScheduledAt);

		public bool IsDisposed => JobTimer == null;
		private readonly Timer JobTimer;

		protected InternalJob(string jobName, DateTime scheduledAt, ObjectParameterWrapper? parameters = null) {
			OnJobLoaded();

			JobName = jobName ?? throw new ArgumentNullException(nameof(jobName));
			ScheduledAt = scheduledAt.Equals(DateTime.MinValue) ? throw new ArgumentOutOfRangeException(nameof(scheduledAt)) : scheduledAt;
			UniqueID = JobName + "/" + new Guid().ToString("N") + "/" + ScheduledAt.Ticks;
			DelayBetweenCalls = Timeout.InfiniteTimeSpan;
			JobParameters = parameters;

			JobTimer = new Timer(
					state => {
						OnJobTriggered(JobParameters);
						Helpers.InBackgroundThread(() => OnJobExecuted(), UniqueID);
					},
					null,
					SpanUntilInitialCall,
					DelayBetweenCalls
			);

			OnJobInitialized();
		}

		protected InternalJob(string jobName, TimeSpan scheduledSpan, ObjectParameterWrapper? parameters = null) {
			OnJobLoaded();

			JobName = jobName ?? throw new ArgumentNullException(nameof(jobName));
			ScheduledAt = new DateTime() + scheduledSpan;
			UniqueID = JobName + "/" + new Guid().ToString("N") + "/" + ScheduledAt.Ticks;
			DelayBetweenCalls = Timeout.InfiniteTimeSpan;
			JobParameters = parameters;

			JobTimer = new Timer(
					state => {
						OnJobTriggered(JobParameters);
						Helpers.InBackgroundThread(() => OnJobExecuted(), UniqueID);
					},
					null,
					SpanUntilInitialCall,
					DelayBetweenCalls
			);

			OnJobInitialized();
		}

		/// <summary>
		/// Disables refiring of this job. (Disposes it after first event)
		/// </summary>
		/// <returns></returns>
		public InternalJob WithoutRecurring() {
			if (IsDisposed) {
				throw new ObjectDisposedException(nameof(InternalJob));
			}

			DelayBetweenCalls = Timeout.InfiniteTimeSpan;
			JobTimer.Change(SpanUntilInitialCall, Timeout.InfiniteTimeSpan);
			return this;
		}

		/// <summary>
		/// Sets if the job should reoccure after initial firing.
		/// </summary>
		/// <param name="delayBetweenCalls">The delay to wait before next and rest of the fires.</param>
		/// <returns></returns>
		public InternalJob ReoccureEvery(TimeSpan delayBetweenCalls) {
			if (IsDisposed) {
				throw new ObjectDisposedException(nameof(InternalJob));
			}

			DelayBetweenCalls = delayBetweenCalls;
			JobTimer.Change(SpanUntilInitialCall, DelayBetweenCalls);
			return this;
		}

		/// <summary>
		/// Fired when this job is loaded and ready to be fired at <see cref="ScheduledAt"/> time.
		/// </summary>
		protected abstract void OnJobInitialized();

		/// <summary>
		/// Fired when <see cref="ScheduledAt"/> time is reached.
		/// </summary>
		/// <param name="objectParameterWrapper">Optional object parameter wrapper.</param>
		protected abstract void OnJobTriggered(ObjectParameterWrapper? objectParameterWrapper);

		/// <summary>
		/// Fired when the job is loaded.
		/// </summary>
		protected abstract void OnJobLoaded();

		protected abstract void OnDisposed();

		private void OnJobExecuted() {
			if (IsRecurring) {
				return;
			}

			WithoutRecurring();
			Dispose();
		}

		/// <summary>
		/// <inheritdoc />
		/// </summary>
		public void Dispose() {
			JobTimer?.Dispose();
		}
	}
}
