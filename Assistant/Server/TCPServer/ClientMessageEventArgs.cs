namespace Assistant.Server.TCPServer {
	public class ClientMessageEventArgs {
		public ClientPayload? Payload { get; set; }

		public ClientMessageEventArgs(ClientPayload payload) => Payload = payload;
	}
}
