namespace Luna.CommandLine.ProcessBase {
	public class SessionOut {
		public readonly string StandardOutput;
		public readonly string ErrorOut;

		internal SessionOut(string stdOut, string stdError) {
			StandardOutput = stdOut ??= "";
			ErrorOut = stdError ??= "";
		}
	}
}
