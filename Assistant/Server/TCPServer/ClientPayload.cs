using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Server.TCPServer {
	public class ClientPayload {
		public string? ClientId { get; set; }
		public string? ClientIp { get; set; }
		public string? RawMessage { get; set; }
		public DateTime ReceviedTime { get; set; }
	}
}
