using static Assistant.Core.Shell.InterpreterCore;

namespace Assistant.Core.Shell.Internal {
	public readonly struct Response {
		public readonly string? ExecutionResult;
		public readonly EXECUTE_RESULT ResultCode;

		public Response(string? resultString, EXECUTE_RESULT resultCode) {
			ExecutionResult = resultString;
			ResultCode = resultCode;
		}
	}
}
