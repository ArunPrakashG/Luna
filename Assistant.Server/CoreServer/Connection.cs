using Assistant.Server.CoreServer.EventArgs;
using Assistant.Server.CoreServer.Requests;
using System;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Server.CoreServer {
	public class Connection : IDisposable {
		private TcpClient Client;
		private CoreServerBase Server;
		private bool IsAlreadyInitialized;
		public string ClientUniqueId { get; private set; }
		public string ClientIpAddress { get; private set; }
		private BaseRequest PreviousRequest;
		private SemaphoreSlim SendSemaphore = new SemaphoreSlim(1, 1);
		public bool IsDisposed { get; private set; }

		public delegate void OnDisconnected(object sender, OnDisconnectedEventArgs e);
		public event OnDisconnected Disconnected;

		public delegate void OnConnected(object sender, OnConnectedEventArgs e);
		public event OnConnected Connected;

		public delegate void OnReceived(object sender, OnReceivedEventArgs e);
		public event OnReceived Received;

		public Connection(TcpClient client, CoreServerBase server) {
			Client = client ?? throw new ArgumentNullException(nameof(client));
			Server = server ?? throw new ArgumentNullException(nameof(server));
			ClientIpAddress = Client.Client.RemoteEndPoint.ToString()?.Split(':')[0].Trim();

			ClientUniqueId = !string.IsNullOrEmpty(ClientIpAddress)
				? GenerateUniqueId(ClientIpAddress)
				: GenerateUniqueId(Client.Client.GetHashCode().ToString());
		}

		public async Task<Connection> Init() {
			if (IsAlreadyInitialized) {
				return this;
			}

			Logger.LogInfo($"Client connected with address -> {ClientIpAddress} / {ClientUniqueId}");

			lock (CoreServerBase.ConnectedClients) {
				if (!CoreServerBase.ConnectedClients.ContainsKey(ClientUniqueId)) {
					CoreServerBase.ConnectedClients.TryAdd(ClientUniqueId, this);
				}
			}

			Connected?.Invoke(this, new OnConnectedEventArgs(ClientIpAddress, DateTime.Now, ClientUniqueId));
			Receive();
			await Task.Delay(800).ConfigureAwait(false);
			IsAlreadyInitialized = true;
			return this;
		}

		private void Receive() {
			Helpers.InBackgroundThread(async () => {
				while (Client.Connected) {
					try {
						if (Client.Available <= 0) {
							await Task.Delay(1).ConfigureAwait(false);
							continue;
						}

						NetworkStream stream = Client.GetStream();

						if (!stream.DataAvailable) {
							await Task.Delay(1).ConfigureAwait(false);
							continue;
						}

						byte[] buffer = new byte[8000];

						if (stream.Read(buffer, 0, buffer.Length) <= 0) {
							await Task.Delay(1).ConfigureAwait(false);
							continue;
						}

						string receviedMessage = Encoding.ASCII.GetString(buffer);
						if (string.IsNullOrEmpty(receviedMessage)) {
							await Task.Delay(1).ConfigureAwait(false);
							continue;
						}

						receviedMessage = Regex.Replace(receviedMessage, "\\0", string.Empty);

						if (string.IsNullOrEmpty(receviedMessage)) {
							await Task.Delay(1).ConfigureAwait(false);
							continue;
						}

						BaseRequest request = new BaseRequest(receviedMessage);

						if (PreviousRequest != null && PreviousRequest.Identifier == request.Identifier) {
							await Task.Delay(1).ConfigureAwait(false);
							continue;
						}

						Received?.Invoke(this, new OnReceivedEventArgs(receviedMessage, DateTime.Now, request, request.Identifier, ClientIpAddress));
					}
					catch (SocketException) {
						//this means client was forcefully disconnected from the remote endpoint. so process it here and disconnect the client and dispose
						await DisconnectAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)), false).ConfigureAwait(false);
						break;
					}
					catch (Exception e) {
						EventLogger.LogException(e);
						continue;
					}
				}
			}, ClientUniqueId, true);
		}

		public async Task<bool> SendAsync(BaseResponse response) {
			if (response == null) {
				return false;
			}

			if (!Helpers.IsSocketConnected(Client.Client)) {
				return false;
			}

			try {
				await SendSemaphore.WaitAsync().ConfigureAwait(false);
				string jsonResponse = BaseResponse.SerializeRequest<BaseResponse>(response);

				if (string.IsNullOrEmpty(jsonResponse)) {
					return false;
				}

				if (Client == null || !Client.Connected || !Client.GetStream().CanWrite) {
					return false;
				}

				NetworkStream stream = Client.GetStream();
				byte[] writeBuffer = Encoding.ASCII.GetBytes(jsonResponse);
				stream.Write(writeBuffer, 0, writeBuffer.Length);
				stream.Dispose();
				return true;
			}
			finally {
				SendSemaphore.Release();
			}
		}

		public async Task<(bool status, BaseRequest request)> SendWithResponseAsync(BaseResponse response, CancellationTokenSource token) {
			if (response == null) {
				return (false, null);
			}

			if (token == null) {
				token = new CancellationTokenSource(TimeSpan.FromSeconds(10));
			}

			try {
				if (!await SendAsync(response).ConfigureAwait(false)) {
					return (false, null);
				}

				while (!token.Token.IsCancellationRequested) {
					if (Client == null || !Client.Connected || !Helpers.IsSocketConnected(Client.Client)) {
						break;
					}

					if (Client.Available <= 0) {
						await Task.Delay(1).ConfigureAwait(false);
						continue;
					}

					NetworkStream stream = Client.GetStream();

					if (!stream.DataAvailable) {
						await Task.Delay(1).ConfigureAwait(false);
						continue;
					}

					byte[] buffer = new byte[8000];

					if (stream.Read(buffer, 0, buffer.Length) <= 0) {
						await Task.Delay(1).ConfigureAwait(false);
						continue;
					}

					string receviedMessage = Encoding.ASCII.GetString(buffer);
					if (string.IsNullOrEmpty(receviedMessage)) {
						await Task.Delay(1).ConfigureAwait(false);
						continue;
					}

					receviedMessage = Regex.Replace(receviedMessage, "\\0", string.Empty);

					if (string.IsNullOrEmpty(receviedMessage)) {
						await Task.Delay(1).ConfigureAwait(false);
						continue;
					}

					BaseRequest request = new BaseRequest(receviedMessage);

					if (PreviousRequest != null && PreviousRequest.Identifier == request.Identifier) {
						await Task.Delay(1).ConfigureAwait(false);
						continue;
					}

					PreviousRequest = request;
					return (true, request);
				}
			}
			finally {
				token.Dispose();
			}

			return (false, null);
		}

		public async Task<bool> DisconnectAsync(CancellationTokenSource tokenSource, bool reconnect) {
			if (tokenSource == null) {
				tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
			}

			while (!tokenSource.Token.IsCancellationRequested) {
				if (Client.GetStream().DataAvailable) {
					await Task.Delay(1).ConfigureAwait(false);
					continue;
				}

				Client.Close();
				Client?.Dispose();
				break;
			}

			tokenSource.Dispose();
			lock (CoreServerBase.ConnectedClients) {
				if (CoreServerBase.ConnectedClients.ContainsKey(ClientUniqueId)) {
					CoreServerBase.ConnectedClients.TryRemove(ClientUniqueId, out _);
				}
			}
			Disconnected?.Invoke(this, new OnDisconnectedEventArgs(ClientUniqueId, DateTime.Now, reconnect));
			return true;
		}

		public void Dispose() {
			if (IsDisposed) {
				return;
			}

			bool isDisconnected = DisconnectAsync(new CancellationTokenSource(TimeSpan.FromSeconds(8)), false).Result;

			if (isDisconnected) {
				Client = null;
				Server = null;
				PreviousRequest = null;
				SendSemaphore?.Release();
				SendSemaphore?.Dispose();
				SendSemaphore = null;
				Disconnected = null;
				Connected = null;
				Received = null;
				IsDisposed = true;
			}
		}

		private static string GenerateUniqueId(string ipAddress) {
			if (string.IsNullOrEmpty(ipAddress)) {
				return string.Empty;
			}

			return ipAddress.ToLowerInvariant().Trim().GetHashCode().ToString();
		}
	}
}
