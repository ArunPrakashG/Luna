using HomeAssistant.Core;
using HomeAssistant.Extensions;
using HomeAssistant.Log;
using RestSharp;
using RestSharp.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Update {

	public class Updater {
		private readonly Logger Logger = new Logger("UPDATER");
		private readonly GitHub Git = new GitHub();
		public bool UpdateAvailable = false;
		private Timer AutoUpdateTimer;
		private readonly Stopwatch ElapasedTimeCalculator = new Stopwatch();
		private DateTime UpdateTimerStartTime;

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
			if (Tess.Config.AutoUpdates && AutoUpdateTimer == null) {
				TimeSpan autoUpdatePeriod = TimeSpan.FromHours(Tess.Config.UpdateIntervalInHours);

				AutoUpdateTimer = new Timer(
					e => CheckAndUpdate(true),
					null,
					autoUpdatePeriod, // Delay
					autoUpdatePeriod // Period
				);

				UpdateTimerStartTime = DateTime.Now;
				ElapasedTimeCalculator.Start();
				Logger.Log($"TESS will automatically check for updates every 5 hours.", LogLevels.Info);
			}
		}

		public (bool, Version) CheckAndUpdate(bool withTimer) {
			if (withTimer && AutoUpdateTimer == null) {
				StartUpdateTimer();
			}

			UpdateTimerStartTime = DateTime.Now;
			ElapasedTimeCalculator.Restart();

			if (!Tess.Config.AutoUpdates) {
				Logger.Log("Updates are disabled.");
				return (false, Constants.Version);
			}

			Rootobject GitRoot = null;

			try {
				string gitToken = Helpers.FetchVariable(0, true, "GITHUB_TOKEN");

				if (!string.IsNullOrEmpty(gitToken) || !string.IsNullOrWhiteSpace(gitToken)) {
					GitRoot = Git.FetchLatestAssest(gitToken);
				}
			}
			catch (NullReferenceException) {
				Logger.Log("Could not fetch the required details from github api.", LogLevels.Warn);
				return (false, Constants.Version);
			}

			string GitVersion;
			if (GitRoot != null) {
				GitVersion = GitRoot.tag_name;
			}
			else {
				Logger.Log("Failed to fetch the required details from github api.", LogLevels.Error);
				return (false, Constants.Version);
			}

			Version LatestVersion;

			try {
				LatestVersion = Version.Parse(GitVersion);
			}
			catch (Exception) {
				Logger.Log("Could not prase the version. Make sure the versioning is correct @ GitHub.", LogLevels.Warn);
				return (false, Constants.Version);
			}

			if (LatestVersion > Constants.Version) {
				Logger.Log($"New version available!");
				Logger.Log($"Latest Version: {LatestVersion} / Local Version: {Constants.Version}");
				Logger.Log("Automatically updating in 10 seconds...");
				UpdateAvailable = true;
				Helpers.InBackground(async () => {
					await Task.Delay(10000).ConfigureAwait(false);
					await InitUpdate().ConfigureAwait(false);
				});
				return (true, LatestVersion);
			}
			else if (LatestVersion < Constants.Version) {
				Logger.Log("Seems like you are on a pre-release channel. please report any bugs you encounter!", LogLevels.Warn);
				return (true, LatestVersion);
			}
			else {
				Logger.Log($"You are up to date! ({LatestVersion}/{Constants.Version})");
				return (true, LatestVersion);
			}
		}

		public async Task InitUpdate() {
			Rootobject GitRoot = null;
			string gitToken;
			try {
				gitToken = Helpers.FetchVariable(0, true, "GITHUB_TOKEN");

				if (!string.IsNullOrEmpty(gitToken) || !string.IsNullOrWhiteSpace(gitToken)) {
					GitRoot = Git.FetchLatestAssest(gitToken);
				}
			}
			catch (NullReferenceException) {
				Logger.Log("Failed to fetch GITHUB_TOKEN variable value from file. file exists ?", LogLevels.Warn);
				return;
			}

			if (GitRoot != null)
			{
				int releaseID = GitRoot.assets[0].id;

				Logger.Log($"Release name: {GitRoot.name}");
				Logger.Log($"URL: {GitRoot.url}", LogLevels.Trace);
				Logger.Log($"Version: {GitRoot.tag_name}", LogLevels.Trace);
				Logger.Log($"Publish time: {GitRoot.published_at.ToLongTimeString()}");
				Logger.Log($"ZIP URL: {GitRoot.assets[0].browser_download_url}", LogLevels.Trace);
				Logger.Log($"Downloading {GitRoot.name}.zip...");

				if (File.Exists(Constants.UpdateZipFileName)) {
					File.Delete(Constants.UpdateZipFileName);
				}

				RestClient client = new RestClient($"{Constants.GitHubAssetDownloadURL}/{releaseID}?access_token={gitToken}");
				RestRequest request = new RestRequest(Method.GET);
				client.UserAgent = Constants.GitHubProjectName;
				request.AddHeader("cache-control", "no-cache");
				request.AddHeader("Accept", "application/octet-stream");
				IRestResponse response = client.Execute(request);

				if (response.StatusCode != HttpStatusCode.OK) {
					Logger.Log("Failed to download. Status Code: " + response.StatusCode + "/" + response.ResponseStatus.ToString());
					return;
				}

				response.RawBytes.SaveAs(Constants.UpdateZipFileName);
			}

			Logger.Log("Sucessfully Downloaded, Starting update process...");
			await Task.Delay(1000).ConfigureAwait(false);

			if (!File.Exists(Constants.UpdateZipFileName)) {
				Logger.Log("Something unknown and fatal has occured during update process. unable to proceed.", LogLevels.Error);
				return;
			}

			if (Directory.Exists(Constants.BackupDirectoryPath)) {
				Directory.Delete(Constants.BackupDirectoryPath, true);
				Logger.Log("Deleted old backup folder and its contents.");
			}

			try {
				if (OS.IsUnix) {
					string executable = Path.Combine(Constants.HomeDirectory, Constants.GitHubProjectName);

					if (File.Exists(executable)) {
						OS.UnixSetFileAccessExecutable(executable);
						Logger.Log("File Permission set successfully!");
					}
				}

				await Task.Delay(1000).ConfigureAwait(false);
				Helpers.ExecuteCommand("cd /home/pi/Desktop/HomeAssistant/Helpers/Updater && dotnet UpdateHelper.dll", true);
				_ = await Helpers.RestartOrExit().ConfigureAwait(false);
			}
			catch (Exception e) {
				Logger.Log(e, LogLevels.Error);
				return;
			}
		}
	}
}
