using AssistantSharedLibrary.Assistant.Servers.TCPServer.EventArgs;
using AssistantSharedLibrary.Logging;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AssistantSharedLibrary.Assistant.Servers.TCPServer {
	public class ServerBase {
		private TcpListener Server { get; set; }
		private int ServerPort { get; set; }
		private bool ExitRequested { get; set; }
		private bool _isListernerEventFired = false;
		private readonly SemaphoreSlim ServerSemaphore = new SemaphoreSlim(1, 1);
		public bool IsServerListerning { get; private set; }
		public static readonly ConcurrentDictionary<string, Connection> ConnectedClients = new ConcurrentDictionary<string, Connection>();
		public static int ConnectedClientsCount => ConnectedClients.Count;

		public delegate void OnClientConnected(object sender, OnClientConnectedEventArgs e);
		public event OnClientConnected ClientConnected;

		public delegate void OnServerStartedListerning(object sender, OnServerStartedListerningEventArgs e);
		public event OnServerStartedListerning ServerStarted;

		public delegate void OnServerShutdown(object sender, OnServerShutdownEventArgs e);
		public event OnServerShutdown ServerShutdown;

		public async Task<ServerBase> Start(int port, int backlog = 10) {
			if (port <= 0 || IsServerListerning) {
				return this;
			}

			ServerPort = port;
			try {
				await ServerSemaphore.WaitAsync().ConfigureAwait(false);
				EventLogger.LogInfo("Starting TCP Server...");
				Server = new TcpListener(new IPEndPoint(IPAddress.Any, ServerPort));
				Server.Start(backlog);

				EventLogger.LogInfo($"Server waiting for connections at port -> {ServerPort}");

				Helpers.InBackgroundThread(async () => {
					while (!ExitRequested && Server != null) {
						IsServerListerning = true;

						if (!_isListernerEventFired) {
							ServerStarted?.Invoke(this, new OnServerStartedListerningEventArgs(IPAddress.Any, ServerPort, DateTime.Now));
							_isListernerEventFired = true;
						}

						if (Server.Pending()) {
							TcpClient client = await Server.AcceptTcpClientAsync().ConfigureAwait(false);
							Connection clientConnection = new Connection(client, this);
							ClientConnected?.Invoke(this, new OnClientConnectedEventArgs(clientConnection.ClientIpAddress, DateTime.Now, clientConnection.ClientUniqueId));
							await clientConnection.Init().ConfigureAwait(false);
						}

						await Task.Delay(1).ConfigureAwait(false);
					}

					IsServerListerning = false;
				}, this.GetHashCode().ToString(), true);

				while (!IsServerListerning) {
					await Task.Delay(1).ConfigureAwait(false);
				}

				return this;
			}
			finally {
				ServerSemaphore.Release();
			}
		}

		public async Task<bool> Shutdown() {
			ExitRequested = true;

			if (Server != null) {
				if (Server.Server.Connected) {
					Server.Stop();
				}

				while (Server.Server.Connected) {
					await Task.Delay(1).ConfigureAwait(false);
				}

				Server = null;
			}

			ServerShutdown?.Invoke(this, new OnServerShutdownEventArgs(DateTime.Now, ExitRequested));
			return true;
		}
	}
}
