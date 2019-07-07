using System;

namespace HomeAssistant.Modules.Interfaces {

	public interface IModuleBase {

		///<summary>
		/// Identifier for the Module.
		///</summary>
		long ModuleIdentifier { get; set; }

		///<summary>
		/// Specifies if the module requires a stable constant internet connection to function.
		///</summary>
		bool RequiresInternetConnection { get; }

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
		bool InitModuleService();

		///<summary>
		/// Invoked to shutdown module.
		///</summary>
		bool InitModuleShutdown();
	}
}
