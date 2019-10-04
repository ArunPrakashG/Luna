using Assistant.Extensions;
using Assistant.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using static Assistant.AssistantCore.Enums;

namespace Assistant.Server.TCPServer {
	public class TCPServerCore {
		private readonly Logger Logger = new Logger("TCP-SERVER");
		private TcpListener? Listener { get; set; }
		private static int ServerPort { get; set; }
		public static bool IsOnline { get; private set; }
		private static bool IsStopRequested { get; set; }

		public static List<Client> Clients { get; private set; } = new List<Client>();

		[Obsolete("Testing only")]
		public TCPServerCore InitTCPServer(int port) {
			ServerPort = port;
			return this;
		}

		public void StartServerCore() {
			Logger.Log("Starting TCP Server...", LogLevels.Trace);
			Listener = new TcpListener(new IPEndPoint(IPAddress.Any, ServerPort));
			Listener.Start(10);
			IsOnline = true;

			Logger.Log("TCP Server listerning for connections...");
			Helpers.InBackgroundThread(async () => {
				while (!IsStopRequested && Listener != null) {
					if (Listener.Pending()) {
						Socket socket = await Listener.AcceptSocketAsync().ConfigureAwait(false);
						if (socket != null) {
							Client client = new Client(socket);

							if (!Clients.Any(x => x.UniqueId == client.UniqueId)) {
								Clients.Add(client);
								Logger.Log("Added to client collection");
							}

							client.Init();

							client.OnMessageRecevied += OnClientMessageRecevied;
							client.OnDisconnected += OnClientDisconnected;
						}
					}
					await Task.Delay(1).ConfigureAwait(false);
				}

				Logger.Log("TCP Server stopped.");
			}, "AlwaysOn Server thread", true);
		}

		private void OnClientDisconnected(object sender, ClientDisconnectedEventArgs e) {
			Logger.Log(e.ClientIp + " " + e.DisconnectDelay);
		}

		private void OnClientMessageRecevied(object sender, ClientMessageEventArgs e) {
			Logger.Log(e.Payload.RawMessage);
		}

		public void StopServer() => IsStopRequested = true;
	}
}
