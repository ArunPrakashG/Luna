using Assistant.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace UpdateHelper {
	internal class Program {
		public static string? HomeDirectory => Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
		private static readonly SemaphoreSlim UpdateSemaphore = new SemaphoreSlim(1, 1);
		private static async Task Main(string[] args) {
			Console.WriteLine("Starting Update Process...");
			Console.WriteLine("1.0.0.0");
			Console.WriteLine("Assistant Directory: " + Directory.GetParent(HomeDirectory).Parent?.FullName + "/Assistant.Core/");
			string updateFilePath = $@"{Directory.GetParent(HomeDirectory).Parent?.FullName}/Assistant.Core/Latest.zip";

			if (!File.Exists(updateFilePath)) {
				Console.WriteLine("Update file not found in the source directory.");
				await Task.Delay(3000).ConfigureAwait(false);
				return;
			}

			using ZipArchive Archive = ZipFile.OpenRead(updateFilePath);

			if (await UpdateFromArchive(Archive, Directory.GetParent(HomeDirectory).Parent?.FullName + "/Assistant.Core/").ConfigureAwait(false)) {
				Console.WriteLine("Successfully Updated! Restarting application...");
				Console.WriteLine("cd /home/pi/Desktop/HomeAssistant/Assistant.Core && dotnet Assistant.Core.dll".ExecuteBash(false));
				Console.WriteLine("Exiting Updater as the process is finished...");
				await Task.Delay(3000).ConfigureAwait(false);
				Environment.Exit(0);
			}
		}

		private static async Task<bool> UpdateFromArchive(ZipArchive archive, string pathtoUpdate) {
			if ((archive == null) || string.IsNullOrEmpty(pathtoUpdate)) {
				Console.WriteLine(nameof(archive) + " || " + nameof(pathtoUpdate));
				return false;
			}

			try {
				await UpdateSemaphore.WaitAsync().ConfigureAwait(false);
				Console.WriteLine("Updating from archive...");
				await Task.Delay(2000).ConfigureAwait(false);
				string BackupDirectory = $"{Directory.GetParent(HomeDirectory).Parent.FullName}/Backups";

				if (!Directory.Exists(BackupDirectory)) {
					Directory.CreateDirectory(BackupDirectory);
				}

				string versionvalue = File.ReadAllLines(pathtoUpdate + "version.txt")[0].Trim();
				Version version = Version.Parse(versionvalue);

				string BackupDirectorySaves = $"{Directory.GetParent(HomeDirectory).Parent?.FullName}/Backups/{version.ToString()}";
				Console.WriteLine("Starting Backup Process...");
				Copy(pathtoUpdate, BackupDirectorySaves);

				Console.WriteLine("Waiting 8 seconds for all remaining process to clear the memory...");
				await Task.Delay(TimeSpan.FromSeconds(8)).ConfigureAwait(false);

				foreach (ZipArchiveEntry zipFile in archive.Entries) {
					string file = Path.Combine(pathtoUpdate, zipFile.FullName);

					switch (file) {
						case "Assistant.json":
						case "Variables.txt":
						case "NLog.config":
						case "GpioConfig.json":
						case "TraceLog.txt":
						case "DiscordBot.json":
						case "MailConfig.json":
							Console.WriteLine($"Ignored file -> {file}");
							continue;
					}

					Console.WriteLine("Updating >>> " + file);
					string? directory = Path.GetDirectoryName(file);

					if (string.IsNullOrEmpty(directory)) {
						Console.WriteLine(nameof(directory));
						return false;
					}

					await Task.Delay(200).ConfigureAwait(false);
					if (!Directory.Exists(directory)) {
						Directory.CreateDirectory(directory);
					}

					if (string.IsNullOrEmpty(zipFile.Name)) {
						continue;
					}

					zipFile.ExtractToFile(file, true);
				}
			}
			catch (Exception e) {
				Console.WriteLine(e);
				return false;
			}
			finally {
				UpdateSemaphore.Release();
			}


			return true;
		}

		private static void Copy(string sourceDirectory, string targetDirectory) {
			DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
			DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);
			CopyAll(diSource, diTarget);
		}

		protected static void CopyAll(DirectoryInfo source, DirectoryInfo target) {
			try {
				Directory.CreateDirectory(target.FullName);
				Console.WriteLine("Backup Directory = " + target.FullName);
				foreach (FileInfo fi in source.GetFiles()) {
					Console.WriteLine(string.Format(@"Copying File >> {0}\{1}", target.FullName, fi.Name));
					Task.Delay(200).Wait();
					fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
				}

				foreach (DirectoryInfo diSourceSubDir in source.GetDirectories()) {
					DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
					Console.WriteLine("Copying Directory >> " + diSourceSubDir.Name);
					CopyAll(diSourceSubDir, nextTargetSubDir);
				}
			}
			catch (Exception ex) {
				Console.WriteLine(ex);
				return;
			}
		}
	}
}
