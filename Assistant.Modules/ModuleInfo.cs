using Assistant.Modules.Interfaces;
using static Assistant.Modules.ModuleInitializer;

namespace Assistant.Modules {
	public class ModuleInfo<TModuleObject> where TModuleObject : IModuleBase {
		public string? ModuleIdentifier { get; set; }
		public MODULE_TYPE ModuleType { get; set; }		
		public TModuleObject Module { get; set; }
		public bool IsLoaded { get; set; }

		public ModuleInfo(string? modId, TModuleObject modObj, MODULE_TYPE modType, bool isLoaded) {
			ModuleIdentifier = modId;
			Module = modObj;
			ModuleType = modType;
			IsLoaded = isLoaded;
		}
	}
}
