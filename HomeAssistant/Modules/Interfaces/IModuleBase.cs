using System;
using System.Collections.Generic;
using System.Text;

namespace HomeAssistant.Modules {
	interface IModuleBase {
		string ModuleName { get; set; }

		Version Version { get; set; }

		void OnModuleStarted();

		void OnModuleShutdown();
	}
}
