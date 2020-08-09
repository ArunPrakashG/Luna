using Luna.Modules.Interfaces;

namespace Luna.Modules {
	internal struct ModuleWrapper<T> where T : IModule {
		internal readonly string? ModuleIdentifier;
		internal readonly T Module;
		internal readonly bool IsLoaded;

		internal ModuleWrapper(string? modId, T modObj, bool isLoaded) {
			ModuleIdentifier = modId;
			Module = modObj;
			IsLoaded = isLoaded;
		}
	}
}
