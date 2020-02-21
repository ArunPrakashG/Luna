namespace Assistant.Extensions.Shared.Shell {
	public readonly struct Parameter {
		public readonly string CommandKey;
		public readonly string[] Parameters;

		public Parameter(string cmdKey, string[] parameters) {
			CommandKey = cmdKey;
			Parameters = parameters;
		}
	}
}
