namespace Luna.Shell {
	public readonly struct Parameter {
		public readonly string CommandKey;
		public readonly string[] Parameters;
		public int ParameterCount => Parameters.Length;

		public Parameter(string cmdKey, string[] parameters) {
			CommandKey = cmdKey;
			Parameters = parameters;
		}
	}
}
