using System;

namespace HomeAssistant.Modules.Interfaces {
	internal interface IModuleBase {
		string ModuleIdentifier { get; set; }
		Version ModuleVersion { get; set; }
		(bool, T) InitModuleService<T>();
		bool InitModuleShutdown();
	}
}
