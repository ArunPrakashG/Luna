
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

using Assistant.Extensions;
using Assistant.Log;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.AssistantCore {

	public class CoreConfig : IEquatable<CoreConfig> {

		[JsonProperty] public bool AutoRestart { get; set; } = false;

		[JsonProperty] public bool AutoUpdates { get; set; } = true;

		[JsonProperty] public bool EnableConfigWatcher { get; set; } = true;

		[JsonProperty] public bool EnableModuleWatcher { get; set; } = true;

		[JsonProperty] public bool EnableModules { get; set; } = true;

		[JsonProperty] public int UpdateIntervalInHours { get; set; } = 5;

		[JsonProperty] public string KestrelServerUrl { get; set; } = "http://localhost:9090";

		[JsonProperty] public int ServerAuthCode { get; set; } = 3033;

		[JsonProperty] public bool PushBulletLogging { get; set; } = true;

		[JsonProperty] public bool KestrelServer { get; set; } = true;

		[JsonProperty] public bool GpioSafeMode { get; set; } = false;

		[JsonProperty] public Dictionary<string, int> AuthenticatedTokens { get; set; }

		[JsonProperty]
		public int[] OutputModePins = new int[]
		{
			2, 3, 4, 17, 27, 22, 10, 9
		};

		[JsonProperty]
		public int[] InputModePins = new int[] {
			26,20
		};

		[JsonProperty] public bool DisplayStartupMenu { get; set; } = false;

		[JsonProperty] public bool EnableGpioControl { get; set; } = true;

		[JsonProperty] public bool Debug { get; set; } = false;

		[JsonProperty] public string ZomatoApiKey { get; set; }

		[JsonProperty] public string StatisticsServerIP { get; set; }

		[JsonProperty] public string OwnerEmailAddress { get; set; } = "arun.prakash.456789@gmail.com";

		[JsonProperty] public bool EnableFirstChanceLog { get; set; } = false;

		[JsonProperty] public bool EnableTextToSpeech { get; set; } = true;

		[JsonProperty] public bool MuteAssistant { get; set; } = false;

		[JsonProperty] public string OpenWeatherApiKey { get; set; } = null;

		[JsonProperty] public string GitHubToken { get; set; }

		[JsonProperty] public string PushBulletApiKey { get; set; } = null;

		[JsonProperty] public string AssistantEmailId { get; set; }

		[JsonProperty] public string AssistantDisplayName { get; set; } = "TESS";

		[JsonProperty] public string AssistantEmailPassword { get; set; }

		[JsonProperty(Required = Required.Default)] public DateTime ProgramLastStartup { get; set; }

		[JsonProperty(Required = Required.Default)] public DateTime ProgramLastShutdown { get; set; }

		[JsonProperty] public bool CloseRelayOnShutdown { get; set; } = false;

		[JsonIgnore] private readonly Logger Logger = new Logger("CORE-CONFIG");
		private static readonly SemaphoreSlim ConfigSemaphore = new SemaphoreSlim(1, 1);

		public CoreConfig SaveConfig(CoreConfig config) {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config folder doesn't exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			Logger.Log("Saving core config...", Enums.LogLevels.Trace);
			string filePath = Constants.CoreConfigPath;
			string json = JsonConvert.SerializeObject(config, Formatting.Indented);
			string newFilePath = filePath + ".new";

			ConfigSemaphore.Wait();

			if (Core.ConfigWatcher.FileSystemWatcher != null) {
				Core.ConfigWatcher.FileSystemWatcher.EnableRaisingEvents = false;
			}

			try {
				File.WriteAllText(newFilePath, json);

				if (File.Exists(filePath)) {
					File.Replace(newFilePath, filePath, null);
				}
				else {
					File.Move(newFilePath, filePath);
				}
			}
			catch (Exception e) {
				Logger.Log(e);
				if (Core.ConfigWatcher.FileSystemWatcher != null) {
					Core.ConfigWatcher.FileSystemWatcher.EnableRaisingEvents = true;
				}
				return config;
			}
			finally {
				ConfigSemaphore.Release();
			}

			if (Core.ConfigWatcher.FileSystemWatcher != null) {
				Core.ConfigWatcher.FileSystemWatcher.EnableRaisingEvents = true;
			}
			Logger.Log("Saved core config!", Enums.LogLevels.Trace);
			return config;
		}

		public CoreConfig LoadConfig() {
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config folder doesn't exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			if (!File.Exists(Constants.CoreConfigPath) && !GenerateDefaultConfig()) {
				return new CoreConfig();
			}

			Logger.Log("Loading core config...", Enums.LogLevels.Trace);
			string JSON;
			ConfigSemaphore.Wait();
			using (FileStream Stream = new FileStream(Constants.CoreConfigPath, FileMode.Open, FileAccess.Read)) {
				using (StreamReader ReadSettings = new StreamReader(Stream)) {
					JSON = ReadSettings.ReadToEnd();
				}
			}

			Core.Config = JsonConvert.DeserializeObject<CoreConfig>(JSON);
			ConfigSemaphore.Release();
			Logger.Log("Core configuration loaded successfully!", Enums.LogLevels.Trace);
			return Core.Config;
		}

		public bool GenerateDefaultConfig() {
			Logger.Log("Core config file doesnt exist. press c to continue generating default config or q to quit.");

			ConsoleKeyInfo? Key = Helpers.FetchUserInputSingleChar(TimeSpan.FromMinutes(1));

			if (!Key.HasValue) {
				Logger.Log("No value has been entered, continuing to run the program...");
			}
			else {
				switch (Key.Value.KeyChar) {
					case 'c':
						break;

					case 'q':
						Task.Run(async () => await Core.Exit().ConfigureAwait(false));
						return false;

					default:
						Logger.Log("Unknown value entered! continuing to run the Core...");
						break;
				}
			}

			Logger.Log("Generating default Config...");
			if (!Directory.Exists(Constants.ConfigDirectory)) {
				Logger.Log("Config directory doesnt exist, creating one...");
				Directory.CreateDirectory(Constants.ConfigDirectory);
			}

			if (File.Exists(Constants.CoreConfigPath)) {
				return true;
			}

			CoreConfig Config = new CoreConfig();
			SaveConfig(Config);
			return true;
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) {
				return false;
			}

			if (ReferenceEquals(this, obj)) {
				return true;
			}

			if (obj.GetType() != this.GetType()) {
				return false;
			}

			return Equals((CoreConfig) obj);
		}

		public override int GetHashCode() {
			unchecked {
				int hashCode = AutoRestart.GetHashCode();
				hashCode = (hashCode * 397) ^ AutoUpdates.GetHashCode();
				hashCode = (hashCode * 397) ^ EnableConfigWatcher.GetHashCode();
				hashCode = (hashCode * 397) ^ UpdateIntervalInHours;
				hashCode = (hashCode * 397) ^ KestrelServer.GetHashCode();
				hashCode = (hashCode * 397) ^ GpioSafeMode.GetHashCode();
				hashCode = (hashCode * 397) ^ (OutputModePins != null ? OutputModePins.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (InputModePins != null ? InputModePins.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ DisplayStartupMenu.GetHashCode();
				hashCode = (hashCode * 397) ^ EnableGpioControl.GetHashCode();
				hashCode = (hashCode * 397) ^ Debug.GetHashCode();
				hashCode = (hashCode * 397) ^ EnableFirstChanceLog.GetHashCode();
				hashCode = (hashCode * 397) ^ EnableTextToSpeech.GetHashCode();
				hashCode = (hashCode * 397) ^ MuteAssistant.GetHashCode();
				hashCode = (hashCode * 397) ^ CloseRelayOnShutdown.GetHashCode();
				hashCode = (hashCode * 397) ^ ServerAuthCode;
				hashCode = (hashCode * 397) ^ (OwnerEmailAddress != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(OwnerEmailAddress) : 0);
				hashCode = (hashCode * 397) ^ (AssistantEmailId != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(AssistantEmailId) : 0);
				hashCode = (hashCode * 397) ^ (AssistantEmailPassword != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(AssistantEmailPassword) : 0);
				hashCode = (hashCode * 397) ^ ProgramLastStartup.GetHashCode();
				hashCode = (hashCode * 397) ^ ProgramLastShutdown.GetHashCode();
				return hashCode;
			}
		}

		public override string ToString() => base.ToString();

		public bool Equals(CoreConfig other) {
			if (ReferenceEquals(null, other)) {
				return false;
			}

			if (ReferenceEquals(this, other)) {
				return true;
			}

			return AutoRestart == other.AutoRestart && AutoUpdates == other.AutoUpdates &&
				   EnableConfigWatcher == other.EnableConfigWatcher &&
				   UpdateIntervalInHours == other.UpdateIntervalInHours &&
				   KestrelServer == other.KestrelServer && GpioSafeMode == other.GpioSafeMode &&
				   Equals(OutputModePins, other.OutputModePins) && Equals(InputModePins, other.InputModePins) &&
				   DisplayStartupMenu == other.DisplayStartupMenu && EnableGpioControl == other.EnableGpioControl &&
				   Debug == other.Debug && EnableFirstChanceLog == other.EnableFirstChanceLog &&
				   EnableTextToSpeech == other.EnableTextToSpeech && MuteAssistant == other.MuteAssistant &&
				   CloseRelayOnShutdown == other.CloseRelayOnShutdown && ServerAuthCode == other.ServerAuthCode &&
				   string.Equals(OwnerEmailAddress, other.OwnerEmailAddress, StringComparison.OrdinalIgnoreCase) &&
				   string.Equals(AssistantEmailId, other.AssistantEmailId, StringComparison.OrdinalIgnoreCase) &&
				   string.Equals(AssistantEmailPassword, other.AssistantEmailPassword, StringComparison.OrdinalIgnoreCase) &&
				   ProgramLastStartup.Equals(other.ProgramLastStartup) &&
				   ProgramLastShutdown.Equals(other.ProgramLastShutdown);
		}

		public static bool operator ==(CoreConfig left, CoreConfig right) => Equals(left, right);

		public static bool operator !=(CoreConfig left, CoreConfig right) => !Equals(left, right);
	}
}
