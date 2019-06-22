using System;
using HomeAssistant.Modules.Interfaces;

namespace HomeAssistant.Modules {
	public class GoogleMap : IModuleBase {

		//TODO
		//get latitude and longitude
		//search locations etc
		public string ModuleIdentifier { get; set; } = nameof(GoogleMap);
		public Version ModuleVersion { get; set; } = new Version("4.9.0.0");

		public GoogleMap() {

		}

		public (bool, GoogleMap) InitModuleService<GoogleMap>() {
			
		}

		public bool InitModuleShutdown() {
			
		}
	}
}
