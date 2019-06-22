using System;

namespace HomeAssistant.Modules.Interfaces {
	internal interface IModuleBase {

		///<summary>
		/// Identifier for the Module.
		///</summary>
		string ModuleIdentifier { get; }

		///<summary>
		/// Author of the Module.
		///</summary>
		string ModuleAuthor { get; }

		///<summary>
		/// Version of the Module.
		///</summary>
		Version ModuleVersion { get; }

		///<summary>
		/// Invoked during module startup.
		///</summary>
		(bool, T) InitModuleService<T>();

		///<summary>
		/// Invoked to shutdown module.
		///</summary>
		bool InitModuleShutdown();
	}
}
