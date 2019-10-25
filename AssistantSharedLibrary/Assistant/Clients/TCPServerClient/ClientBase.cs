using AssistantSharedLibrary.Assistant.Clients.TCPServerClient.EventArgs;
using AssistantSharedLibrary.Assistant.Servers.TCPServer.Requests;
using AssistantSharedLibrary.Assistant.Servers.TCPServer.Responses;
using AssistantSharedLibrary.Logging;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AssistantSharedLibrary.Assistant.Clients.TCPServerClient {
	public class ClientBase : IDisposable {
		private TcpClient Connector;
		private readonly SemaphoreSlim ClientSemaphore = new SemaphoreSlim(1, 1);
		private readonly SemaphoreSlim ClientReceivingSemaphore = new SemaphoreSlim(1, 1);
		private BaseResponse PreviousResponse { get; set; }

		public delegate void OnDisconnected(object sender, OnDisconnectedEventArgs e);
		public event OnDisconnected Disconnected;

		public delegate void OnCommandReceived(object sender, OnCommandReceivedEventArgs e);
		public event OnCommandReceived CommandReceived;

		public delegate void OnConnected(object sender, OnConnectedEventArgs e);
		public event OnConnected Connected;

		public string ServerIP { get; private set; }
		public int ServerPort { get; private set; }
		public bool IsConnected => Connector != null && Connector.Connected;
		public bool IsReceiving { get; private set; }
		public readonly bool ClientInitialized;

		public ClientBase(string ip, int port) {
			if (string.IsNullOrEmpty(ip) || port <= 0) {
				ClientInitialized = false;
				return;
			}

			ServerIP = ip;
			ServerPort = port;
			ClientInitialized = true;
		}

		/// <summary>
		/// The ClientBase startup method.
		/// </summary>
		/// <returns></returns>
		public async Task<ClientBase> Start() {
			if (string.IsNullOrEmpty(ServerIP) || ServerPort <= 0) {
				EventLogger.LogError("Cannot start as either server ip or port is invalid.");
				return this;
			}

			if (IsConnected) {
				return this;
			}

			try {
				await ClientSemaphore.WaitAsync().ConfigureAwait(false);

				if (!Helpers.IsServerOnline(ServerIP)) {
					EventLogger.LogError("Server is offline.");
					return this;
				}

				Connector = new TcpClient(ServerIP, ServerPort);
				EventLogger.LogInfo("Connected to server.");
				Connected?.Invoke(this, new OnConnectedEventArgs(DateTime.Now, ServerIP, ServerPort));

				Helpers.InBackgroundThread(async () => {
					await ClientReceivingSemaphore.WaitAsync().ConfigureAwait(false);

					while (IsConnected) {
						try {
							NetworkStream stream = Connector.GetStream();
							IsReceiving = true;

							if (!stream.DataAvailable) {
								await Task.Delay(1).ConfigureAwait(false);
								continue;
							}

							byte[] readBuffer = new byte[8000];
							int dataCount = stream.Read(readBuffer, 0, readBuffer.Length);

							if (dataCount <= 0 || readBuffer.Length <= 0) {
								await Task.Delay(1).ConfigureAwait(false);
								continue;
							}

							string received = Encoding.ASCII.GetString(readBuffer);

							if (string.IsNullOrEmpty(received)) {
								await Task.Delay(1).ConfigureAwait(false);
								continue;
							}

							BaseResponse receivedObj = BaseResponse.DeserializeRequest<BaseResponse>(received);

							if (PreviousResponse != null && PreviousResponse.Equals(receivedObj)) {
								await Task.Delay(1).ConfigureAwait(false);
								continue;
							}

							CommandReceived?.Invoke(this, new OnCommandReceivedEventArgs(DateTime.Now, receivedObj, received));
							PreviousResponse = receivedObj;
						}
						catch (SocketException s) {
							EventLogger.LogTrace($"SOCKET EXCEPTION -> {s.SocketErrorCode.ToString()}");
							break;
						}
						catch (Exception e) {
							EventLogger.LogError($"EXCEPTION -> {e.Message}");
							break;
						}
					}

					ClientReceivingSemaphore.Release();
					EventLogger.LogInfo("Disconnected from server.");
					IsReceiving = false;
					Disconnected?.Invoke(this, new OnDisconnectedEventArgs(DateTime.Now, false, ServerIP, ServerPort, true));
				}, "Client Receiving Thread", true);

				return this;
			}
			finally {
				ClientSemaphore.Release();
			}
		}

		public async Task<bool> Stop() {
			if (Connector == null || !Connector.Connected) {
				return true;
			}

			while (Connector.GetStream().DataAvailable) {
				await Task.Delay(2).ConfigureAwait(false);
			}

			Connector.Close();

			while (IsConnected) {
				await Task.Delay(2).ConfigureAwait(false);
			}

			return true;
		}

		public async Task<bool> SendAsync(BaseRequest request) {
			if (request == null || Connector == null) {
				return false;
			}

			if (!IsConnected || !Helpers.IsSocketConnected(Connector.Client)) {
				return false;
			}

			try {
				await ClientSemaphore.WaitAsync().ConfigureAwait(false);
				string jsonResponse = BaseRequest.SerializeRequest<BaseRequest>(request);

				if (string.IsNullOrEmpty(jsonResponse)) {
					return false;
				}

				if (!Connector.GetStream().CanWrite) {
					return false;
				}

				NetworkStream stream = Connector.GetStream();
				byte[] writeBuffer = Encoding.ASCII.GetBytes(jsonResponse);
				stream.Write(writeBuffer, 0, writeBuffer.Length);
				stream.Dispose();
				return true;
			}
			finally {
				ClientSemaphore.Release();
			}
		}

		public async Task<(bool status, BaseResponse request)> SendWithResponseAsync(BaseRequest request, CancellationTokenSource token) {
			if (request == null || Connector == null) {
				return (false, null);
			}

			if (token == null) {
				token = new CancellationTokenSource(TimeSpan.FromSeconds(10));
			}

			try {
				if (!await SendAsync(request).ConfigureAwait(false)) {
					return (false, null);
				}

				while (!token.Token.IsCancellationRequested) {
					if (!IsConnected || !Helpers.IsSocketConnected(Connector.Client)) {
						break;
					}

					if (Connector.Available <= 0) {
						await Task.Delay(1).ConfigureAwait(false);
						continue;
					}

					NetworkStream stream = Connector.GetStream();

					if (!stream.DataAvailable) {
						await Task.Delay(1).ConfigureAwait(false);
						continue;
					}

					byte[] readBuffer = new byte[8000];
					int dataCount = stream.Read(readBuffer, 0, readBuffer.Length);

					if (dataCount <= 0 || readBuffer.Length <= 0) {
						await Task.Delay(1).ConfigureAwait(false);
						continue;
					}

					string received = Encoding.ASCII.GetString(readBuffer);

					if (string.IsNullOrEmpty(received)) {
						await Task.Delay(1).ConfigureAwait(false);
						continue;
					}

					BaseResponse receivedObj = BaseResponse.DeserializeRequest<BaseResponse>(received);

					if (PreviousResponse != null && PreviousResponse.Equals(receivedObj)) {
						await Task.Delay(1).ConfigureAwait(false);
						continue;
					}

					PreviousResponse = receivedObj;
					return (true, receivedObj);
				}
			}
			finally {
				token.Dispose();
			}

			return (false, null);
		}
	}
}
