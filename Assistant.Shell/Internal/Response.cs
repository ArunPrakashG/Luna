using static Assistant.Shell.InterpreterCore;

namespace Assistant.Shell.Internal {
	public readonly struct Response {
		public readonly string? ExecutionResult;
		public readonly EXECUTE_RESULT ResultCode;

		public Response(string? resultString, EXECUTE_RESULT resultCode) {
			ExecutionResult = resultString;
			ResultCode = resultCode;
		}
	}
}
