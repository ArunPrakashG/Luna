using HomeAssistant.Log;
using System;
using HomeAssistant.Modules.Interfaces;

namespace MiscModule {

	//TODO for setting remainders and such small activites
	public class MiscModule : IModuleBase, IMiscModule {
		private Logger Logger = new Logger("MISC-MODULE");
		public string ModuleIdentifier => nameof(MiscModule);
		public bool RequiresInternetConnection { get; set; }
		public string ModuleAuthor => "Arun";
		public Version ModuleVersion => new Version("4.9.0.0");

		public MiscModule() {
		}

		public bool InitModuleService() {
			RequiresInternetConnection = false;
			return true;
		}

		public bool InitModuleShutdown() {
			return true;
		}

		public DateTime ConvertTo24Hours(DateTime source) {
			bool sucess = DateTime.TryParse(source.ToString("yyyy MMMM d HH:mm:ss tt"), out DateTime result);
			if (sucess) {
				return result;
			}
			else {
				return DateTime.Now;
			}
		}

		public DateTime ConvertTo12Hours(DateTime source) {
			bool sucess = DateTime.TryParse(source.ToString("dddd, dd MMMM yyyy"), out DateTime result);
			if (sucess) {
				return result;
			}
			else {
				return DateTime.Now;
			}
		}
	}
}
