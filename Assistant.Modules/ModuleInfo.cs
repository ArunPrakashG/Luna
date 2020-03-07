using Assistant.Modules.Interfaces;

namespace Assistant.Modules {
	public struct ModuleInfo<T> where T : IModuleBase {
		public readonly string? ModuleIdentifier;
		public readonly T Module;
		public readonly bool IsLoaded;

		public ModuleInfo(string? modId, T modObj, bool isLoaded) {
			ModuleIdentifier = modId;
			Module = modObj;
			IsLoaded = isLoaded;
		}
	}
}
