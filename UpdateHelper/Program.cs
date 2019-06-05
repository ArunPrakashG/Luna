using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;

namespace UpdateHelper {

	internal class Program {

		public static string HomeDirectory => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

		private static void Main(string[] args) {
			Console.WriteLine("Starting Update Process...");
			Console.WriteLine("1.0.0.0");
			Console.WriteLine("TESS Directory: " + Directory.GetParent(HomeDirectory).Parent.FullName + "/AssistantCore/");
			ZipArchive Archive = null;

			try {
				Archive = ZipFile.OpenRead($@"{Directory.GetParent(HomeDirectory).Parent.FullName + "/AssistantCore/"}Latest.zip");
			}
			catch (Exception e) {
				Console.WriteLine($"{e.Source}/{e.Message}/{e.InnerException}");
				Environment.Exit(0);
			}

			bool Updated = UpdateFromArchive(Archive, Directory.GetParent(HomeDirectory).Parent.FullName + "/AssistantCore/").Result;

			if (Updated) {
				Console.WriteLine("Sucessfully Updated! Restarting application...");
				ExecuteCommand("cd /home/pi/Desktop/HomeAssistant/AssistantCore && dotnet HomeAssistant.dll", false);
				Console.WriteLine("Exiting Updater as the process is finished...");
				Environment.Exit(0);
			}
		}

		public static void ExecuteCommand(string command, bool redirectOutput = false) {
			Process proc = new Process();
			proc.StartInfo.FileName = "/bin/bash";
			proc.StartInfo.Arguments = "-c \" " + command + " \"";

			if (redirectOutput) {
				proc.StartInfo.UseShellExecute = false;
				proc.StartInfo.RedirectStandardOutput = true;
			}

			proc.Start();

			if (redirectOutput) {
				while (!proc.StandardOutput.EndOfStream) {
					Console.WriteLine(">>> " + proc.StandardOutput.ReadLine());
				}
			}
		}

		private static async Task<bool> UpdateFromArchive(ZipArchive archive, string pathtoUpdate) {
			if ((archive == null) || string.IsNullOrEmpty(pathtoUpdate)) {
				Console.WriteLine(nameof(archive) + " || " + nameof(pathtoUpdate));
				return false;
			}

			Console.WriteLine("Updating from archive...");

			await Task.Delay(2000).ConfigureAwait(false);

			string BackupDirectory = $"{Directory.GetParent(HomeDirectory).Parent.FullName}/Backups";

			if (!Directory.Exists(BackupDirectory)) {
				Directory.CreateDirectory(BackupDirectory);
			}

			Version version = null;

			try {
				string versionvalue = File.ReadAllLines(pathtoUpdate + "version.txt")[0].Trim();
				version = Version.Parse(versionvalue);
			}
			catch (Exception e) {
				Console.WriteLine(e);
			}

			string BackupDirectorySaves = $"{Directory.GetParent(HomeDirectory).Parent.FullName}/Backups/{version.ToString()}";
			Console.WriteLine("Starting Backup Process...");
			Copy(pathtoUpdate, BackupDirectorySaves);

			Console.WriteLine("Waiting 8 seconds for all remaing process to clear the memory...");
			await Task.Delay(8000).ConfigureAwait(false);

			try {
				foreach (ZipArchiveEntry zipFile in archive.Entries) {
					string file = Path.Combine(pathtoUpdate, zipFile.FullName);

					if (file.Equals(@"TESS.json") || file.Equals("Variables.txt") || file.Equals("NLog.config") || file.Equals("GPIOConfig.json")
						|| file.Equals("TraceLog.txt")) {
						Console.WriteLine("Ignored " + file + " file.");
						continue;
					}

					Console.WriteLine("Updating >>> " + file);
					string directory = Path.GetDirectoryName(file);

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
