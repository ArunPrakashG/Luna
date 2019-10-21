using Assistant.Extensions;
using Assistant.Log;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static Assistant.AssistantCore.Enums;

namespace Assistant.Server.TCPServer {
	public class TCPServerCore {
		private static readonly Logger Logger = new Logger("TCP-SERVER");
		private TcpListener? Listener { get; set; }
		private static int ServerPort { get; set; }
		private static bool IsStopRequested { get; set; }
		private static readonly SemaphoreSlim ListSemaphore = new SemaphoreSlim(1, 1);

		public static List<Client> Clients { get; private set; } = new List<Client>();
		public static bool IsOnline { get; private set; }

		public TCPServerCore InitTCPServer(int port) {
			ServerPort = port;
			return this;
		}

		public TCPServerCore StartServerCore() {
			Logger.Log("Starting TCP Server...", LogLevels.Trace);
			Listener = new TcpListener(new IPEndPoint(IPAddress.Any, ServerPort));
			Listener.Start(10);
			IsOnline = true;

			Logger.Log("TCP Server listerning for connections...");
			Helpers.InBackgroundThread(async () => {
				while (!IsStopRequested && Listener != null) {
					if (Listener.Pending()) {
						Socket socket = await Listener.AcceptSocketAsync().ConfigureAwait(false);
						Client client = new Client(socket);
						client.ThreadInfo = Helpers.InBackgroundThread(async () => await client.Init().ConfigureAwait(false), client.GetHashCode().ToString(), true);
					}

					await Task.Delay(1).ConfigureAwait(false);
				}
			}, "AlwaysOn Server thread", true);

			return this;
		}

		private async Task DisposeServer() {
			if (Clients != null && Clients.Count > 0) {
				foreach (Client client in Clients) {
					if (client == null) {
						continue;
					}

					await client.DisconnectClientAsync(true).ConfigureAwait(false);
				}
			}

			if (Listener != null) {
				Listener.Stop();
				Listener = null;
				Logger.Log("TCP Server stopped.");
			}
		}

		public static void AddClient(Client client) {
			if (client == null) {
				return;
			}

			try {
				ListSemaphore.Wait();

				if (Clients.Any(x => x.UniqueId != null && x.UniqueId.Equals(client.UniqueId))) {
					return;
				}

				if (!Clients.Contains(client)) {
					Clients.Add(client);
					Logger.Log("Added to client collection");
				}
			}
			finally {
				ListSemaphore.Release();
			}
		}

		public static void RemoveClient(Client client) {
			if (client == null) {
				return;
			}

			try {
				ListSemaphore.Wait();
				if (!Clients.Any(x => x.UniqueId != null && x.UniqueId.Equals(client.UniqueId))) {
					return;
				}

				if (Clients.Contains(client)) {
					Clients.Remove(client);
					Logger.Log("Removed from client collection");
				}
			}
			finally {
				ListSemaphore.Release();
			}
		}

		public void StopServer() {
			IsStopRequested = true;
			Helpers.InBackground(DisposeServer);
		}
	}
}
