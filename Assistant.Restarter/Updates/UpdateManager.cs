using Luna.CommandLine;
using Luna.Modules;
using Luna.Modules.Interfaces;
using Synergy.Extensions;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Luna.External.Updates {
	internal class UpdateManager : IDisposable {
		private static readonly SemaphoreSlim UpdateSemaphore = new SemaphoreSlim(1, 1);
		private readonly HttpClient Client;
		private readonly UpdateJob UpdateJob;

		internal bool UpdateAvailable { get; private set; }
		internal bool IsOnPrerelease { get; private set; }

		internal UpdateManager() {
			Client = new HttpClient();
			Client.DefaultRequestHeaders.Add("cache-control", "no-cache");
			Client.DefaultRequestHeaders.Add("Accept", "application/octet-stream");
			Client.DefaultRequestHeaders.Add("User-Agent", Constants.GitProjectName);
			UpdateJob = new UpdateJob(TimeSpan.FromDays(1), null);
			UpdateJob.ReoccureEvery(TimeSpan.FromDays(1));			
		}

		internal async Task CheckAndUpdate() {
			if (!Core.IsNetworkAvailable) {
				return;
			}

			if (!Core.IsUpdatesAllowed) {
				Logger.Info("Updates are disabled.");
				return;
			}

			UpdateResult result = await GetUpdateDataIfAvailable().ConfigureAwait(false) ?? new UpdateResult(null, false);
			if (!Version.TryParse(result.Response.ReleaseTagName, out Version? latestVersion)) {
				return;
			}

			if (!UpdateAvailable) {
				Logger.Info($"You are up to date! ({latestVersion}/{Constants.Version})");
				return;
			}

			Logger.Info($"New version available!");
			Logger.Info($"Latest Version: {latestVersion} / Local Version: {Constants.Version}");
			Logger.Info("Automatically updating in 10 seconds...");
			ModuleLoader.ExecuteActionOnType<IEvent>((e) => e.OnUpdateAvailable());
			ModuleLoader.ExecuteActionOnType<IAsyncEvent>(async (e) => await e.OnUpdateAvailable().ConfigureAwait(false));
			string? updateFileName = await DownloadLatestVersionAsync(result).ConfigureAwait(false);

			if (string.IsNullOrEmpty(updateFileName)) {
				return;
			}

			await InitUpdateAsync(updateFileName).ConfigureAwait(false);
		}

		private async Task<GithubResponse> GetResponseAsync() => await new GithubResponse().LoadAsync().ConfigureAwait(false);

		private async Task<UpdateResult?> GetUpdateDataIfAvailable() {
			if (!Core.IsNetworkAvailable || !Core.IsUpdatesAllowed) {
				return null;
			}

			Logger.Info("Checking for any new version...");

			try {
				GithubResponse response = await GetResponseAsync().ConfigureAwait(false);

				string? gitVersion = response.ReleaseTagName;

				if (string.IsNullOrEmpty(gitVersion)) {
					Logger.Warn("Failed to request version information.");
					return null;
				}

				if (!Version.TryParse(gitVersion, out Version? latestVersion)) {
					Logger.Warn("Could not parse the version. Make sure the version is correct at Github project repo.");
					return null;
				}

				UpdateAvailable = latestVersion > Constants.Version;
				IsOnPrerelease = latestVersion < Constants.Version;
				return new UpdateResult(response, UpdateAvailable);
			}
			catch (Exception e) {
				Logger.Error(e.ToString());
				return null;
			}
		}

		private async Task<string?> DownloadLatestVersionAsync(UpdateResult response) {
			if (response.Response == null || response.Response.Assets == null || response.Response.Assets[0] == null) {
				return null;
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

				using (HttpResponseMessage result = await Client.GetAsync($"{Constants.GitDownloadUrl}/{releaseID}").ConfigureAwait(false)) {
					if (!result.IsSuccessStatusCode) {
						Logger.Warn($"Download failed. {result.StatusCode}/{result.ReasonPhrase}");
						return null;
					}

					Logger.Info("Successfully Downloaded, writing to file...");
					await File.WriteAllBytesAsync(updateFileName, await result.Content.ReadAsByteArrayAsync().ConfigureAwait(false)).ConfigureAwait(false);

					if (!File.Exists(updateFileName)) {
						Logger.Warn($"Failed to write data to '{updateFileName}' file.");
						return null;
					}

					return updateFileName;
				}
			}
			catch (Exception e) {
				Logger.Error(e.ToString());
				return null;
			}
			finally {
				UpdateSemaphore.Release();
			}
		}

		private async Task InitUpdateAsync(string updateFileName) {
			if (string.IsNullOrEmpty(updateFileName)) {
				return;
			}

			if (OS.IsUnix) {
				string executable = Path.Combine(Constants.HomeDirectory, Constants.GitProjectName + ".dll");

				if (File.Exists(executable)) {
					OS.UnixSetFileAccessExecutable(executable);
					Logger.Info("File Permission set successfully!");
				}
			}

			ModuleLoader.ExecuteActionOnType<IEvent>((e) => e.OnUpdateStarted());
			ModuleLoader.ExecuteActionOnType<IAsyncEvent>(async (e) => await e.OnUpdateStarted().ConfigureAwait(false));

			await Update(updateFileName).ConfigureAwait(false);

			// starting luna again after update
			using (OSCommandLineInterfacer cl = new OSCommandLineInterfacer(OSPlatform.Linux, true)) {
				cl.Execute($"cd {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Luna")}");
				cl.Execute("dotnet Luna.dll");
			}
		}

		private async Task Update(string updateFileName) {
			if (string.IsNullOrEmpty(updateFileName) || !File.Exists(updateFileName)) {
				return;
			}

			await UpdateSemaphore.WaitAsync().ConfigureAwait(false);
			CreateBackup();

			try {
				using (ZipArchive archive = new ZipArchive(new FileStream(updateFileName, FileMode.Open, FileAccess.ReadWrite), ZipArchiveMode.Update)) {
					foreach (var zipEntry in archive.Entries) {
						switch (zipEntry.Name) {
							case "Commands":
							case "Luna.json":
							case "NLog.config":
							case "GpioConfig.json":
							case "TraceLog.txt":
							case "DiscordBot.json":
							case "MailConfig.json":
								Logger.Info("Ignored file -> " + zipEntry.Name);
								continue;
						}

						string updateTargetPath = Path.Combine(Constants.HomeDirectory, zipEntry.FullName);
						zipEntry.ExtractToFile(updateTargetPath, true);
						Logger.Info("Updated -> " + zipEntry.Name);
					}
				}
			}
			catch (Exception e) {
				Logger.Error(e.ToString());
				return;
			}
			finally {
				UpdateSemaphore.Release();
			}
		}

		private void CreateBackup() {
			Logger.Info("Starting Backup Process...");
			const string backupDirectory = "CoreBackup";

			if (!Directory.Exists(backupDirectory)) {
				Directory.CreateDirectory(backupDirectory);
			}

			string versionValue = File.ReadAllLines(Path.Combine(Constants.HomeDirectory, "version.txt")).FirstOrDefault().Trim();
			Version version = Version.Parse(versionValue);

			string coreBackupDirectoryPath = Path.Combine(Constants.HomeDirectory, backupDirectory, versionValue);

			if (Directory.Exists(coreBackupDirectoryPath)) {
				Directory.Delete(coreBackupDirectoryPath, true);
			}

			Directory.CreateDirectory(coreBackupDirectoryPath);
			Copy(Constants.HomeDirectory, coreBackupDirectoryPath);
		}

		private static void Copy(string sourceDirectory, string targetDirectory) {
			DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
			DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);
			CopyAll(diSource, diTarget);
		}

		private static void CopyAll(DirectoryInfo source, DirectoryInfo target) {
			try {
				Directory.CreateDirectory(target.FullName);
				Logger.Info("Backup Directory = " + target.FullName);
				foreach (FileInfo fi in source.GetFiles()) {
					Logger.Info(string.Format(@"Copying File >> {0}\{1}", target.FullName, fi.Name));
					Task.Delay(200).Wait();
					fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
				}

				foreach (DirectoryInfo diSourceSubDir in source.GetDirectories()) {
					DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
					Logger.Info("Copying Directory >> " + diSourceSubDir.Name);
					CopyAll(diSourceSubDir, nextTargetSubDir);
				}
			}
			catch (Exception ex) {
				Logger.Error(ex.ToString());
				return;
			}
		}

		public void Dispose() {
			Client?.Dispose();
			UpdateJob?.Dispose();
		}
	}
}
