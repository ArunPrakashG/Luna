using System;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace Luna {
	internal class SingleInstanceMutexLocker : IDisposable {
		private bool HasHandle = false;
		private Mutex? Mutex;

		private void InitMutex(string? mutexName) {
			if (string.IsNullOrEmpty(mutexName)) {
				mutexName = Assembly.GetExecutingAssembly().GetName().Name ??= nameof(Luna);
			}

			Guid uniqueId = new Guid(mutexName);
			Mutex = new Mutex(false, string.Format("{0}_{1}", mutexName, uniqueId.ToString()));

			var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
			var securitySettings = new MutexSecurity();
			securitySettings.AddAccessRule(allowEveryoneRule);
			Mutex.SetAccessControl(securitySettings);
		}

		internal bool TryAquireLock(int timeOut = 10000, string? mutexName = null) {
			InitMutex(mutexName);

			if (Mutex == null) {
				throw new ApplicationException($"Fatal exception occured internally. {nameof(SingleInstanceMutexLocker)} can't be null.");
			}

			try {
				HasHandle = timeOut < 0 ? Mutex.WaitOne(Timeout.Infinite, false) : Mutex.WaitOne(timeOut, false);

				if (HasHandle == false) {
					throw new TimeoutException($"Timed out waiting for exclusive access on {nameof(SingleInstanceMutexLocker)}");
				}
			}
			catch (AbandonedMutexException) {
				HasHandle = true;
			}

			return HasHandle;
		}

		/// <summary>
		/// <inheritdoc />
		/// </summary>
		public void Dispose() {
			if(Mutex == null) {
				return;
			}

			if (HasHandle) {
				Mutex.ReleaseMutex();
			}

			Mutex.Close();
		}
	}
}
