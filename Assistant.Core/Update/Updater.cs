using Assistant.Extensions;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using RestSharp;
using RestSharp.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static Assistant.Logging.Enums;

namespace Assistant.Core.Update {

	public class Updater {
		private readonly ILogger Logger = new Logger("UPDATER");
		private GitHub Git = new GitHub();
		public bool UpdateAvailable = false;
		private Timer? AutoUpdateTimer;
		private readonly Stopwatch ElapasedTimeCalculator = new Stopwatch();
		private DateTime UpdateTimerStartTime;
		private static readonly SemaphoreSlim UpdateSemaphore = new SemaphoreSlim(1, 1);

		public void StopUpdateTimer() {
			if (AutoUpdateTimer != null) {
				AutoUpdateTimer.Dispose();
				AutoUpdateTimer = null;
			}

			if (ElapasedTimeCalculator != null && ElapasedTimeCalculator.IsRunning) {
				ElapasedTimeCalculator.Stop();
			}
		}

		public TimeSpan ElapsedUpdateTime() {
			if (ElapasedTimeCalculator.IsRunning) {
				return ElapasedTimeCalculator.Elapsed;
			}
			else {
				Logger.Log("Elapsed time calculator isn't running, cant get the time left.", LogLevels.Warn);
				return new TimeSpan();
			}
		}

		public int HoursUntilNextCheck() => (DateTime.Now - UpdateTimerStartTime).Hours;

		public void StartUpdateTimer() {
			StopUpdateTimer();
			if (Core.Config.AutoUpdates && AutoUpdateTimer == null) {
				TimeSpan autoUpdatePeriod = TimeSpan.FromHours(Core.Config.UpdateIntervalInHours);

				AutoUpdateTimer = new Timer(
					async e => await CheckAndUpdateAsync(true).ConfigureAwait(false),
					null,
					autoUpdatePeriod, // Delay
					autoUpdatePeriod // Period
				);

				UpdateTimerStartTime = DateTime.Now;
				ElapasedTimeCalculator.Start();
				Logger.Log($"{Core.AssistantName} will automatically check for updates every 5 hours.", LogLevels.Info);
			}
		}

		public async Task<(bool, Version?)> CheckAndUpdateAsync(bool withTimer) {
			if (!Core.IsNetworkAvailable) {
				return (false, Constants.Version);
			}

			if (withTimer && AutoUpdateTimer == null) {
				StartUpdateTimer();
			}

			await UpdateSemaphore.WaitAsync().ConfigureAwait(false);
			Logger.Log("Checking for any new version...", LogLevels.Trace);
			UpdateTimerStartTime = DateTime.Now;
			ElapasedTimeCalculator.Restart();

			if (!Core.Config.AutoUpdates) {
				Logger.Log("Updates are disabled.", LogLevels.Trace);
				UpdateSemaphore.Release();
				return (false, Constants.Version);
			}

			Git = Git.FetchLatestAssest();
			string GitVersion = !string.IsNullOrEmpty(Git.ReleaseTagName) ? Git.ReleaseTagName : string.Empty;

			if (string.IsNullOrEmpty(GitVersion)) {
				Logger.Log("Failed to fetch the required details from github api.", LogLevels.Error);
				UpdateSemaphore.Release();
				return (false, Constants.Version);
			}

			if (!Version.TryParse(GitVersion, out Version? LatestVersion)) {
				Logger.Log("Could not parse the version. Make sure the versioning is correct @ GitHub.", LogLevels.Warn);
				UpdateSemaphore.Release();
				return (false, Constants.Version);
			}

			if (LatestVersion > Constants.Version) {
				UpdateAvailable = true;
				Logger.Log($"New version available!", LogLevels.Green);
				Logger.Log($"Latest Version: {LatestVersion} / Local Version: {Constants.Version}");
				Logger.Log("Automatically updating in 10 seconds...", LogLevels.Warn);
				await Core.ModuleLoader.ExecuteAsyncEvent(Modules.ModuleInitializer.MODULE_EXECUTION_CONTEXT.UpdateAvailable).ConfigureAwait(false);
				Helpers.ScheduleTask(async () => await InitUpdate().ConfigureAwait(false), TimeSpan.FromSeconds(10));
				UpdateSemaphore.Release();
				return (true, LatestVersion);
			}

			if (LatestVersion < Constants.Version) {
				Logger.Log("Seems like you are on a pre-release channel. please report any bugs you encounter!", LogLevels.Warn);
				UpdateSemaphore.Release();
				return (true, LatestVersion);
			}

			Logger.Log($"You are up to date! ({LatestVersion}/{Constants.Version})");
			UpdateSemaphore.Release();
			return (true, LatestVersion);
		}

		public async Task<bool> InitUpdate() {
			if (Git == null || Git.Assets == null || Git.Assets.Length <= 0 || Git.Assets[0] == null) {
				return false;
			}

			await UpdateSemaphore.WaitAsync().ConfigureAwait(false);
			int releaseID = Git.Assets[0].AssetId;
			Logger.Log($"Release name: {Git.ReleaseFileName}");
			Logger.Log($"URL: {Git.ReleaseUrl}", LogLevels.Trace);
			Logger.Log($"Version: {Git.ReleaseTagName}", LogLevels.Trace);
			Logger.Log($"Publish time: {Git.PublishedAt.ToLongTimeString()}");
			Logger.Log($"ZIP URL: {Git.Assets[0].AssetDownloadUrl}", LogLevels.Trace);
			Logger.Log($"Downloading {Git.ReleaseFileName}.zip...");

			if (File.Exists(Constants.UpdateZipFileName)) {
				File.Delete(Constants.UpdateZipFileName);
			}

			RestClient client = new RestClient($"{Constants.GitHubAssetDownloadURL}/{releaseID}");
			RestRequest request = new RestRequest(Method.GET);
			client.UserAgent = Constants.GitHubProjectName;
			request.AddHeader("cache-control", "no-cache");
			request.AddHeader("Accept", "application/octet-stream");
			IRestResponse response = client.Execute(request);

			if (response.StatusCode != HttpStatusCode.OK) {
				Logger.Log("Failed to download. Status Code: " + response.StatusCode + "/" + response.ResponseStatus.ToString());
				UpdateSemaphore.Release();
				return false;
			}

			response.RawBytes.SaveAs(Constants.UpdateZipFileName);

			Logger.Log("Successfully Downloaded, Starting update process...");
			await Task.Delay(2000).ConfigureAwait(false);

			if (!File.Exists(Constants.UpdateZipFileName)) {
				Logger.Log("Something unknown and fatal has occurred during update process. unable to proceed.", LogLevels.Error);
				UpdateSemaphore.Release();
				return false;
			}

			if (Directory.Exists(Constants.BackupDirectoryPath)) {
				Directory.Delete(Constants.BackupDirectoryPath, true);
				Logger.Log("Deleted old backup folder and its contents.");
			}

			if (OS.IsUnix) {
				if (string.IsNullOrEmpty(Constants.HomeDirectory)) {
					return false;
				}

				string executable = Path.Combine(Constants.HomeDirectory, Constants.GitHubProjectName);

				if (File.Exists(executable)) {
					OS.UnixSetFileAccessExecutable(executable);
					Logger.Log("File Permission set successfully!");
				}
			}

			UpdateSemaphore.Release();
			await Task.Delay(1000).ConfigureAwait(false);
			await Core.ModuleLoader.ExecuteAsyncEvent(Modules.ModuleInitializer.MODULE_EXECUTION_CONTEXT.UpdateStarted).ConfigureAwait(false);
			"cd /home/pi/Desktop/HomeAssistant/Helpers/Updater && dotnet UpdateHelper.dll".ExecuteBash(true);
			await Core.Restart(5).ConfigureAwait(false);
			return true;
		}
	}
}
