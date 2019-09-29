
//    _  _  ___  __  __ ___     _   ___ ___ ___ ___ _____ _   _  _ _____
//   | || |/ _ \|  \/  | __|   /_\ / __/ __|_ _/ __|_   _/_\ | \| |_   _|
//   | __ | (_) | |\/| | _|   / _ \\__ \__ \| |\__ \ | |/ _ \| .` | | |
//   |_||_|\___/|_|  |_|___| /_/ \_\___/___/___|___/ |_/_/ \_\_|\_| |_|
//

//MIT License

//Copyright(c) 2019 Arun Prakash
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;
using RestSharp;
using RestSharp.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Update {

	public class Updater {
		private readonly Logger Logger = new Logger("UPDATER");
		private GitHub Git = new GitHub();
		public bool UpdateAvailable = false;
		private Timer AutoUpdateTimer;
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

			Logger.Log("Update timer disposed.", Enums.LogLevels.Trace);
		}

		public TimeSpan ElapsedUpdateTime() {
			if (ElapasedTimeCalculator.IsRunning) {
				return ElapasedTimeCalculator.Elapsed;
			}
			else {
				Logger.Log("Elapsed time calculator isn't running, cant get the time left.", Enums.LogLevels.Warn);
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
				Logger.Log($"{Core.AssistantName} will automatically check for updates every 5 hours.", Enums.LogLevels.Info);
			}
		}

		public async Task<(bool, Version)> CheckAndUpdateAsync(bool withTimer) {
			if (!Core.IsNetworkAvailable) {
				return (false, Constants.Version);
			}

			if (withTimer && AutoUpdateTimer == null) {
				StartUpdateTimer();
			}

			await UpdateSemaphore.WaitAsync().ConfigureAwait(false);
			Logger.Log("Checking for any new version...", Enums.LogLevels.Trace);
			UpdateTimerStartTime = DateTime.Now;
			ElapasedTimeCalculator.Restart();

			if (!Core.Config.AutoUpdates) {
				Logger.Log("Updates are disabled.", Enums.LogLevels.Trace);
				UpdateSemaphore.Release();
				return (false, Constants.Version);
			}

			Git = Git.FetchLatestAssest();
			string GitVersion = !Helpers.IsNullOrEmpty(Git.ReleaseTagName) ? Git.ReleaseTagName : string.Empty;

			if (Helpers.IsNullOrEmpty(GitVersion)) {
				Logger.Log("Failed to fetch the required details from github api.", Enums.LogLevels.Error);
				UpdateSemaphore.Release();
				return (false, Constants.Version);
			}

			if (!Version.TryParse(GitVersion, out Version LatestVersion)) {
				Logger.Log("Could not prase the version. Make sure the versioning is correct @ GitHub.", Enums.LogLevels.Warn);
				UpdateSemaphore.Release();
				return (false, Constants.Version);
			}

			if (LatestVersion > Constants.Version) {
				UpdateAvailable = true;
				Logger.Log($"New version available!", Enums.LogLevels.Success);
				Logger.Log($"Latest Version: {LatestVersion} / Local Version: {Constants.Version}");
				Logger.Log("Automatically updating in 10 seconds...", Enums.LogLevels.Warn);
				await Core.ModuleLoader.ExecuteAsyncEvent(Enums.AsyncModuleContext.UpdateAvailable).ConfigureAwait(false);
				Helpers.ScheduleTask(async () => await InitUpdate().ConfigureAwait(false), TimeSpan.FromSeconds(10));
				UpdateSemaphore.Release();
				return (true, LatestVersion);
			}

			if (LatestVersion < Constants.Version) {
				Logger.Log("Seems like you are on a pre-release channel. please report any bugs you encounter!", Enums.LogLevels.Warn);
				UpdateSemaphore.Release();
				return (true, LatestVersion);
			}

			Logger.Log($"You are up to date! ({LatestVersion}/{Constants.Version})");
			UpdateSemaphore.Release();
			return (true, LatestVersion);
		}

		public async Task<bool> InitUpdate() {
			if (Git == null) {
				return false;
			}

			await UpdateSemaphore.WaitAsync().ConfigureAwait(false);
			int releaseID = Git.Assets[0].AssetId;
			Logger.Log($"Release name: {Git.ReleaseFileName}");
			Logger.Log($"URL: {Git.ReleaseUrl}", Enums.LogLevels.Trace);
			Logger.Log($"Version: {Git.ReleaseTagName}", Enums.LogLevels.Trace);
			Logger.Log($"Publish time: {Git.PublishedAt.ToLongTimeString()}");
			Logger.Log($"ZIP URL: {Git.Assets[0].AssetDownloadUrl}", Enums.LogLevels.Trace);
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


			Logger.Log("Sucessfully Downloaded, Starting update process...");
			await Task.Delay(2000).ConfigureAwait(false);

			if (!File.Exists(Constants.UpdateZipFileName)) {
				Logger.Log("Something unknown and fatal has occured during update process. unable to proceed.", Enums.LogLevels.Error);
				UpdateSemaphore.Release();
				return false;
			}

			if (Directory.Exists(Constants.BackupDirectoryPath)) {
				Directory.Delete(Constants.BackupDirectoryPath, true);
				Logger.Log("Deleted old backup folder and its contents.");
			}

			if (OS.IsUnix) {
				string executable = Path.Combine(Constants.HomeDirectory, Constants.GitHubProjectName);

				if (File.Exists(executable)) {
					OS.UnixSetFileAccessExecutable(executable);
					Logger.Log("File Permission set successfully!");
				}
			}

			UpdateSemaphore.Release();
			await Task.Delay(1000).ConfigureAwait(false);
			await Core.ModuleLoader.ExecuteAsyncEvent(Enums.AsyncModuleContext.UpdateStarted).ConfigureAwait(false);
			Helpers.ExecuteCommand("cd /home/pi/Desktop/HomeAssistant/Helpers/Updater && dotnet UpdateHelper.dll", true);
			return await Helpers.RestartOrExit().ConfigureAwait(false);
		}
	}
}
