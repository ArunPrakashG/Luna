using System;

namespace HomeAssistant.Modules {

	internal interface IModuleBase {
		string ModuleName { get; set; }

		Version Version { get; set; }

		void OnModuleStarted();

		void OnModuleShutdown();
	}
}
