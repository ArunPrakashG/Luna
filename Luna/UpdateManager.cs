using FluentScheduler;
using Luna.Extensions;
using Luna.Logging;
using Luna.Modules.Interfaces.EventInterfaces;
using Luna.Server;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static Luna.Modules.ModuleInitializer;

namespace Luna {
	internal class UpdateResult {
		internal bool IsUpdateAvailable;
		internal readonly GithubResponse? Response;

		internal UpdateResult(GithubResponse? response, bool isUpdateAvailable = false) {			
			Response = response;
		}
	}

	internal class UpdateManager : IDisposable {
		private const string JOB_NAME = "GITHUB_UPDATER";
		private readonly InternalLogger Logger = new InternalLogger(nameof(UpdateManager));
		private static readonly SemaphoreSlim UpdateSemaphore = new SemaphoreSlim(1, 1);
		private readonly HttpClient Client;
		private readonly Core Core;

		internal bool UpdateAvailable { get; private set; }
		internal bool IsOnPrerelease { get; private set; }

		internal DateTime NextUpdateCheck => JobManager.GetSchedule(JOB_NAME).NextRun;

		internal UpdateManager(Core _core) {
			Core = _core ?? throw new ArgumentNullException(nameof(_core));
			Client = new HttpClient();
			Client.DefaultRequestHeaders.Add("cache-control", "no-cache");
			Client.DefaultRequestHeaders.Add("Accept", "application/octet-stream");
			Client.DefaultRequestHeaders.Add("User-Agent", Constants.GitHubProjectName);
		}

		internal async Task<Version?> CheckAndUpdateAsync(bool withTimer) {
			if (!Core.IsNetworkAvailable) {
				return null;
			}

			if (!Core.Config.AutoUpdates) {
				Logger.Info("Updates are disabled.");
				return null;
			}

			UpdateResult result = await IsUpdateAvailable().ConfigureAwait(false);

			if (!Version.TryParse(result.Response.ReleaseTagName, out Version? latestVersion)) {
				return null;
			}

			if (!UpdateAvailable) {
				if (IsOnPrerelease) {
					Logger.Warn("Seems like you are on a pre-release channel. please report any bugs you encounter!");
				}

				Logger.Info($"You are up to date! ({latestVersion}/{Constants.Version})");

				if (withTimer) {
					if (JobManager.GetSchedule(JOB_NAME) == null) {
						JobManager.AddJob(async () => await CheckAndUpdateAsync(withTimer).ConfigureAwait(false), (s) => s.WithName(JOB_NAME).ToRunEvery(1).Days().At(00, 00));
						Logger.Info($"{nameof(Luna)} will check for updates every 24 hours.");
					}
				}

				return latestVersion;
			}

			if (IsOnPrerelease) {
				Logger.Warn("Seems like you are on a pre-release channel. please report any bugs you encounter!");
				return latestVersion;
			}

			Logger.Info($"New version available!");
			Logger.Info($"Latest Version: {latestVersion} / Local Version: {Constants.Version}");
			Logger.Info("Automatically updating in 10 seconds...");
			ExecuteAsyncEvent<IEvent>(MODULE_EXECUTION_CONTEXT.UpdateAvailable, default);
			await DownloadLatestVersionAsync(result).ConfigureAwait(false);
			return latestVersion;
		}

		private async Task<GithubResponse> GetResponseAsync() => await new GithubResponse().LoadAsync().ConfigureAwait(false);

		private async Task<UpdateResult> IsUpdateAvailable() {
			if (!Core.IsNetworkAvailable || !Core.Config.AutoUpdates) {
				return new UpdateResult(null);
			}
			
			Logger.Trace("Checking for any new version...");

			try {
				GithubResponse response = await GetResponseAsync().ConfigureAwait(false);

				if(response == null) {
					return new UpdateResult(null);
				}

				string? gitVersion = response.ReleaseTagName;

				if (string.IsNullOrEmpty(gitVersion)) {
					Logger.Warn("Failed to request version information.");
					return new UpdateResult(null);
				}

				if (!Version.TryParse(gitVersion, out Version? latestVersion)) {
					Logger.Warn("Could not parse the version. Make sure the version is correct at Github project repo.");
					return new UpdateResult(null);
				}

				UpdateAvailable = latestVersion > Constants.Version;
				IsOnPrerelease = latestVersion < Constants.Version;
				return new UpdateResult(response, UpdateAvailable);
			}
			catch(Exception e) {
				Logger.Exception(e);
				return new UpdateResult(null);
			}
		}

		private async Task<bool> DownloadLatestVersionAsync(UpdateResult response) {
			if (response.Response == null || response.Response.Assets == null || response.Response.Assets[0] == null) {
				return false;
			}

			await UpdateSemaphore.WaitAsync().ConfigureAwait(false);

			try {
				int releaseID = response.Response.Assets[0].AssetId;
				Logger.Info($"Release name: {response.Response.ReleaseFileName}");
				Logger.Info($"URL: {response.Response.ReleaseUrl}");
				Logger.Info($"Version: {response.Response.ReleaseTagName}");
				Logger.Info($"Publish time: {response.Response.PublishedAt.ToLongTimeString()}");
				Logger.Info($"ZIP URL: {response.Response.Assets[0].AssetDownloadUrl}");
				Logger.Info($"Downloading {response.Response.ReleaseFileName}.zip...");

				string updateFileName = response.Response.ReleaseFileName + ".zip";
				if (File.Exists(updateFileName)) {
					File.Delete(updateFileName);
				}

				using(HttpResponseMessage result = await Client.GetAsync($"{Constants.GitHubAssetDownloadURL}/{releaseID}").ConfigureAwait(false)) {
					if (!result.IsSuccessStatusCode) {
						Logger.Warn($"Download failed. {result.StatusCode}/{result.ReasonPhrase}");
						return false;
					}

					Logger.Info("Successfully Downloaded, writing to file...");
					await File.WriteAllBytesAsync(updateFileName, await result.Content.ReadAsByteArrayAsync().ConfigureAwait(false)).ConfigureAwait(false);

					if (!File.Exists(updateFileName)) {
						Logger.Warn($"Failed to write data to '{updateFileName}' file.");
						return false;
					}

					Helpers.InBackground(async () => await InitUpdateAsync(updateFileName).ConfigureAwait(false));
					return true;
				}
			}
			catch (Exception e) {
				Logger.Exception(e);
				return false;
			}
			finally {
				UpdateSemaphore.Release();
			}
		}

		private async Task InitUpdateAsync(string updateFileName) {
			if (string.IsNullOrEmpty(updateFileName)) {
				return;
			}

			const string backupDirectory = "Backup_Temp";
			if (Directory.Exists(backupDirectory)) {
				Directory.Delete(backupDirectory, true);
				Logger.Info("Deleted old backup folder and its contents.");
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

			await Task.Delay(1000).ConfigureAwait(false);
			ExecuteAsyncEvent<IEvent>(MODULE_EXECUTION_CONTEXT.UpdateStarted, default);
			"cd /home/pi/Desktop/HomeAssistant/Helpers/Updater && dotnet Assistant.Updater.dll".ExecuteBash(false);
			await Core.Restart(5).ConfigureAwait(false);
			return true;
		}

		public void Dispose() {
			Client?.Dispose();
		}
	}
}
