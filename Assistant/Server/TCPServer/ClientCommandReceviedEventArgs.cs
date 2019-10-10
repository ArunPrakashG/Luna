using static Assistant.Server.TCPServer.CommandEnums;

namespace Assistant.Server.TCPServer {
	public class ClientCommandReceviedEventArgs {
		public CommandType CommandType { get; set; }
		public Command? Command { get; set; }
		public string[]? CommandArguments { get; set; }

		public ClientCommandReceviedEventArgs(CommandType commandType, Command? command, string[]? args) {
			CommandType = commandType;
			Command = command;
			CommandArguments = args;
		}
	}
}
