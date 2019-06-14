using System;

namespace HomeAssistant.Modules.Interfaces {

	internal interface IModuleBase {

		string ModuleName { get; set; }

		Version Version { get; set; }

		void ModuleStart();

		void ModuleExit();
	}
}
