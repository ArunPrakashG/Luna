using System;
using HomeAssistant.Modules.Interfaces;

namespace GoogleMap {
	public class GoogleMap : IModuleBase, IGoogleMap {

		//TODO
		//get latitude and longitude
		//search locations etc
		public GoogleMap MapInstance { get; set; }
		public bool RequiresInternetConnection { get; set; }
		public string ModuleIdentifier { get; set; } = nameof(GoogleMap);
		public Version ModuleVersion { get; set; } = new Version("4.9.0.0");
		public string ModuleAuthor { get; set; } = "Arun";

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
