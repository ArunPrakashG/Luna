using System;
using static Luna.Modules.ModuleEnums;

namespace Luna.Modules.Interfaces {
	public interface IModule {

		///<summary>
		/// Identifier for the Module.
		///</summary>
		public abstract string ModuleIdentifier { get; internal set; }

		/// <summary>
		/// The type of module.
		/// </summary>
		public abstract ModuleType ModuleType { get; }

		///<summary>
		/// Specifies if the module requires a stable constant Internet connection to function.
		///</summary>
		public abstract bool RequiresInternetConnection { get; }

		/// <summary>
		/// Path to module assembly.
		/// </summary>
		public abstract string ModulePath { get; }

		///<summary>
		/// Author of the Module.
		///</summary>
		public abstract string ModuleAuthor { get; }

		///<summary>
		/// Version of the Module.
		///</summary>
		public abstract Version ModuleVersion { get; }

		/// <summary>
		/// Indicates if the module loaded successfully.
		/// </summary>
		public abstract bool IsLoaded { get; set; }

		///<summary>
		/// Invoked to start module service.
		///</summary>
		public abstract bool InitModuleService();

		///<summary>
		/// Invoked to shutdown module.
		///</summary>
		public abstract bool InitModuleShutdown();
	}
}
