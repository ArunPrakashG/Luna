using Assistant.Extensions;
using Assistant.Log;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Assistant.AssistantCore.Enums;

namespace Assistant.Server.TCPServer {
	public class Client {
		public string? UniqueId { get; private set; }
		public string? IpAddress { get; set; }
		public Socket ClientSocket { get; set; }
		public bool DisconnectConnection { get; set; }
		public EndPoint? ClientEndPoint { get; set; }
		private readonly Logger Logger = new Logger("CLIENT");
		internal (int?, Thread?) ThreadInfo;
		public delegate void OnClientMessageRecevied(object sender, ClientMessageEventArgs e);
		public event OnClientMessageRecevied? OnMessageRecevied;

		public delegate void OnClientDisconnected(object sender, ClientDisconnectedEventArgs e);
		public event OnClientDisconnected? OnDisconnected;
		private ClientPayload CachedPayload { get; set; } = new ClientPayload() {
			RawMessage = string.Empty
		};

		public Client(Socket sock) {
			ClientSocket = sock ?? throw new ArgumentNullException(nameof(sock), "Socket cannot be null!");
			ClientEndPoint = ClientSocket.RemoteEndPoint;
			IpAddress = ClientEndPoint?.ToString()?.Split(':')[0].Trim();

			UniqueId = IpAddress != null && !IpAddress.IsNull()
				? GenerateUniqueId(IpAddress)
				: GenerateUniqueId(ClientSocket.GetHashCode().ToString());
		}

		public async Task Init() {
			Logger.Log($"Connected client IP => {IpAddress} / {UniqueId}", LogLevels.Info);
			TCPServerCore.AddClient(this);
			await RecevieAsync().ConfigureAwait(false);
		}

		public async Task DisconnectClientAsync(bool dispose = false) {
			DisconnectConnection = true;

			if (ClientSocket != null) {
				ClientSocket.Disconnect(true);
			}

			if (ClientSocket != null) {
				while (ClientSocket.Connected) {
					Logger.Log("Waiting for client to disconnect...");
					await Task.Delay(5).ConfigureAwait(false);
				}
			}

			$"Disconnected client -> {UniqueId} / {IpAddress}".LogInfo(Logger);
			if (IpAddress != null && UniqueId != null) {
				OnDisconnected?.Invoke(this, new ClientDisconnectedEventArgs(IpAddress, UniqueId, 5000));
			}

			if (dispose) {
				if (ClientSocket != null) {
					ClientSocket.Close();
					ClientSocket.Dispose();
				}

				Helpers.ScheduleTask(() => {
					TCPServerCore.RemoveClient(this);
				}, TimeSpan.FromSeconds(5));
			}
		}

		private async Task RecevieAsync() {
			while (!DisconnectConnection) {
				try {
					if (ClientSocket.Available <= 0) {
						await Task.Delay(2).ConfigureAwait(false);
						continue;
					}

					if (!ClientSocket.Connected) {
						await DisconnectClientAsync(true).ConfigureAwait(false);
					}

					byte[] buffer = new byte[5024];
					int i = ClientSocket.Receive(buffer);

					if (i <= 0) {
						await Task.Delay(1).ConfigureAwait(false);
						continue;
					}

					string receviedMessage = Encoding.ASCII.GetString(buffer);					
					if (Helpers.IsNullOrEmpty(receviedMessage)) {
						await Task.Delay(1).ConfigureAwait(false);
						continue;
					}

					receviedMessage = Regex.Replace(receviedMessage, "\\0", string.Empty);

					ClientPayload payload = new ClientPayload() {
						RawMessage = receviedMessage,
						ClientId = UniqueId,
						ClientIp = IpAddress,
						ReceviedTime = DateTime.Now
					};

					if (payload.RawMessage.Equals(CachedPayload.RawMessage, StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					await OnRecevied(payload).ConfigureAwait(false);
					CachedPayload = payload;
					await Task.Delay(1).ConfigureAwait(false);
				}
				catch (SocketException) {
					//this means client was forcefully disconnected from the remote endpoint. so process it here and disconnect the client and dispose
					await DisconnectClientAsync().ConfigureAwait(false);
					break;
				}
				catch (ThreadAbortException) {
					//This means the client is disconnected and the threat is aborted.
					break;
				}
				catch (Exception e) {
					Logger.Log(e);
					continue;
				}
			}
		}

		private async Task OnRecevied(ClientPayload payload) {
			if (payload == null || payload.RawMessage == null || Helpers.IsNullOrEmpty(payload.RawMessage)) {
				return;
			}

			$"Recevied a message from -> {UniqueId} -> {payload.RawMessage}".LogInfo(Logger);
			OnMessageRecevied?.Invoke(this, new ClientMessageEventArgs(payload));

			if (payload.RawMessage.Equals("DISCONNECT", StringComparison.OrdinalIgnoreCase)) {
				await DisconnectClientAsync(true).ConfigureAwait(false);
				return;
			}

			//TODO process the message and send the reply
			//TODO: just send the recevied message back for now
			await OnProcessedResult(payload.RawMessage).ConfigureAwait(false);
		}

		private async Task OnProcessedResult(string response) {
			if (Helpers.IsNullOrEmpty(response)) {
				return;
			}

			if (!Helpers.IsSocketConnected(ClientSocket)) {
				Logger.Log("Failed to send response as client is disconnected.", LogLevels.Warn);
				await DisconnectClientAsync().ConfigureAwait(false);
				return;
			}

			await SendResponseAsync(response).ConfigureAwait(false);
		}

		private async Task SendResponseAsync(string msg) {
			if (msg == null || msg.IsNull()) {
				return;
			}

			if (!Helpers.IsSocketConnected(ClientSocket)) {
				Logger.Log("Failed to send response as client is disconnected.", LogLevels.Warn);
				await DisconnectClientAsync().ConfigureAwait(false);
				return;
			}

			ClientSocket.Send(Encoding.ASCII.GetBytes(msg));
		}

		public static string GenerateUniqueId(string ipAddress) {
			if (Helpers.IsNullOrEmpty(ipAddress)) {
				return string.Empty;
			}

			return ipAddress.ToLowerInvariant().Trim().GetHashCode().ToString();
		}
	}
}
