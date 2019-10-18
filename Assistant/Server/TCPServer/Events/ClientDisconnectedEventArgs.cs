namespace Assistant.Server.TCPServer.Events {
	public class ClientDisconnectedEventArgs {
		public string? ClientIp { get; set; }
		public string? UniqueId { get; set; }

		public double DisconnectDelay { get; set; }

		public ClientDisconnectedEventArgs(string _clientIp, string _uniqueId, double _delay) {
			ClientIp = _clientIp;
			UniqueId = _uniqueId;
			DisconnectDelay = _delay;
		}
	}
}
