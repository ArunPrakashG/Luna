using Assistant.Server.TCPServer.Commands;
using System;

namespace Assistant.Server.TCPServer.Events {
	public class ClientCommandReceviedEventArgs {
		public DateTime ReceviedTime { get; set; }
		public CommandBase CommandBase { get; set; }

		public ClientCommandReceviedEventArgs(DateTime dt, CommandBase _cmdBase) {
			ReceviedTime = dt;
			CommandBase = _cmdBase;
		}
	}
}
