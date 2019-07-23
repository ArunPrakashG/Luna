using Assistant.Log;
using Assistant.Modules.Interfaces;
using System;

namespace MiscModule {

	//TODO for setting remainders and such small activites
	public class MiscModule : IModuleBase, IMiscModule {
		private Logger Logger = new Logger("MISC-MODULE");

		public long ModuleIdentifier { get; set; }

		public bool RequiresInternetConnection { get; set; }

		public string ModuleAuthor => "Arun Prakash";

		public Version ModuleVersion => new Version("5.0.0.0");

		public MiscModule() {
		}

		public bool InitModuleService() {
			RequiresInternetConnection = false;
			return true;
		}

		public bool InitModuleShutdown() {
			return true;
		}
	}
}
