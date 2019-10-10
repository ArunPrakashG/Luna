using static Assistant.Server.TCPServer.CommandEnums;

namespace Assistant.Server.TCPServer {
	//Command format
	//COMMAND_TYPE|COMMAND|ARGS1~ARGS2~ARGS3~ARGS4 ....
	internal class CommandStructure {
		public CommandType CommandType { get; set; }
		public Command? Command { get; set; }
		public string[]? CommandArguments { get; set; }

		internal CommandStructure(CommandType commandType, Command? command, string[]? args) {
			CommandType = commandType;
			Command = command;
			CommandArguments = args;
		}

		internal CommandStructure() {

		}
	}
}
