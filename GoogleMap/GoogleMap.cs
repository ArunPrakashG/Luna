using System;
using HomeAssistant.AssistantCore;
using HomeAssistant.Log;
using HomeAssistant.Modules.Interfaces;

namespace GoogleMap {
	public class GoogleMap : IModuleBase, IGoogleMap {

		//TODO
		//get latitude and longitude
		//search locations etc
		public GoogleMap MapInstance { get; set; }
		public bool RequiresInternetConnection { get; set; }
		public long ModuleIdentifier { get; set; }
		public Version ModuleVersion { get; set; } = new Version("5.0.0.0");
		public string ModuleAuthor { get; set; } = "Arun Prakash";

		private Logger Logger = new Logger("G-MAP");

		public GoogleMap() {
		}

		public bool InitModuleService() {
			RequiresInternetConnection = true;
			MapInstance = this;
			return true;
		}

		public bool InitModuleShutdown() {
			return true;
		}
	}
}
