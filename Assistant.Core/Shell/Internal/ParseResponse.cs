namespace Assistant.Core.Shell.Internal {
	public readonly struct ParseResponse {
		public readonly bool CommandStatus;
		public readonly string? CommandResponse;

		public ParseResponse(bool status, string? resp) {
			CommandStatus = status;
			CommandResponse = resp;
		}
	}
}
