using System;

namespace Assistant.Modules.Interfaces {

	public interface IModuleBase {

		///<summary>
		/// Identifier for the Module.
		///</summary>
		string ModuleIdentifier { get; set; }

		/// <summary>
		/// The type of module. (0, 1, 2, 3, 4 - Discord, Email, Steam, Youtube, Events respectively)
		/// </summary>
		int ModuleType { get; set; }

		///<summary>
		/// Specifies if the module requires a stable constant Internet connection to function.
		///</summary>
		bool RequiresInternetConnection { get; }

		/// <summary>
		/// Path to module assembly.
		/// </summary>
		string ModulePath { get; set; }

		///<summary>
		/// Author of the Module.
		///</summary>
		string ModuleAuthor { get; }

		///<summary>
		/// Version of the Module.
		///</summary>
		Version ModuleVersion { get; }

		/// <summary>
		/// Indicates if the module loaded successfully.
		/// </summary>
		bool IsLoaded { get; set; }

		///<summary>
		/// Invoked to start module service.
		///</summary>
		bool InitModuleService();

		///<summary>
		/// Invoked to shutdown module.
		///</summary>
		bool InitModuleShutdown();
	}
}
