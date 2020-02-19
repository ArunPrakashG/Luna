using Assistant.Client.EventArgs;
using Assistant.Extensions;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using Assistant.Server.CoreServer.Requests;
using Assistant.Server.CoreServer.Responses;
using Newtonsoft.Json;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Client {
	public class ClientBase : IDisposable {
		private readonly ILogger Logger = new Logger(typeof(ClientBase).Name);
		private TcpClient? Connector;
		public const int MAX_CONNECTION_RETRY_COUNT = 6;
		private readonly SemaphoreSlim ClientSemaphore = new SemaphoreSlim(1, 1);
		private readonly SemaphoreSlim ClientReceivingSemaphore = new SemaphoreSlim(1, 1);
		private BaseResponse? PreviousResponse { get; set; }

		public delegate void OnDisconnected(object sender, OnDisconnectedEventArgs e);
		public event OnDisconnected? Disconnected;

		public delegate void OnResponseReceived(object sender, OnResponseReceivedEventArgs e);
		public event OnResponseReceived? ResponseReceived;

		public delegate void OnConnected(object sender, OnConnectedEventArgs e);
		public event OnConnected? Connected;

		public string? ServerIP { get; private set; }
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
		public async Task<ClientBase> StartAsync() {
			if (string.IsNullOrEmpty(ServerIP) || ServerPort <= 0) {
				Logger.Error("Cannot start as either server ip or port is invalid.");
				return this;
			}

			if (IsConnected) {
				Logger.Error("Client is already connected with the server.");
				return this;
			}

			int connTries = 0;

			try {
				await ClientSemaphore.WaitAsync().ConfigureAwait(false);

				while (connTries < MAX_CONNECTION_RETRY_COUNT) {
					if (!Helpers.IsServerOnline(ServerIP)) {
						Logger.Error($"Server is offline. RETRY_COUNT -> {connTries}");
						connTries++;
						continue;
					}

					try {
						Connector = new TcpClient(ServerIP, ServerPort);
					}
					catch (SocketException) {
						connTries++;
						continue;
					}
					catch (Exception e) {
						Logger.Exception(e);
						break;
					}

					if (IsConnected) {
						break;
					}

					connTries++;
				}

				if (connTries >= MAX_CONNECTION_RETRY_COUNT && !IsConnected) {
					Logger.Error($"Could not connect with server even after {connTries} retry count.");
					return this;
				}

				if (!IsConnected) {
					Logger.Error($"Could not connect with server. Server might be offline or unreachable!");
					return this;
				}

				Logger.Info("Connected to server.");
				Connected?.Invoke(this, new OnConnectedEventArgs(DateTime.Now, ServerIP, ServerPort));

				Helpers.InBackgroundThread(async () => {
					await ClientReceivingSemaphore.WaitAsync().ConfigureAwait(false);

					while (IsConnected) {
						try {
							NetworkStream? stream = Connector?.GetStream();

							if(stream == null) {
								continue;
							}

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

							BaseResponse receivedObj = JsonConvert.DeserializeObject<BaseResponse>(received);

							if (PreviousResponse != null && PreviousResponse.Equals(receivedObj)) {
								await Task.Delay(1).ConfigureAwait(false);
								continue;
							}

							ResponseReceived?.Invoke(this, new OnResponseReceivedEventArgs(DateTime.Now, receivedObj, received));
							PreviousResponse = receivedObj;
						}
						catch (SocketException s) {
							Logger.Trace($"SOCKET EXCEPTION -> {s.SocketErrorCode.ToString()}");
							break;
						}
						catch (Exception e) {
							Logger.Error($"EXCEPTION -> {e.Message}");
							continue;
						}
					}

					ClientReceivingSemaphore.Release();
					Logger.Info("Disconnected from server.");
					IsReceiving = false;
					Disconnected?.Invoke(this, new OnDisconnectedEventArgs(DateTime.Now, false, ServerIP, ServerPort, true));
				}, "Client Receiving Thread", true);

				return this;
			}
			finally {
				ClientSemaphore.Release();
			}
		}

		public async Task<bool> StopAsync() {
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

		public async Task<bool> TryReconnect() {
			if (await StopAsync().ConfigureAwait(false)) {
				await StartAsync().ConfigureAwait(false);

				if (IsConnected) {
					return true;
				}
			}

			return false;
		}

		public async Task<bool> SendAsync(BaseRequest request) {
			if (request == null || Connector == null) {
				return false;
			}

			if (!IsConnected || !Helpers.IsSocketConnected(Connector.Client)) {
				if (!await TryReconnect().ConfigureAwait(false)) {
					return false;
				}
			}

			try {
				await ClientSemaphore.WaitAsync().ConfigureAwait(false);
				string jsonResponse = JsonConvert.SerializeObject(request);

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

		public async Task<(bool status, BaseResponse? request)> SendWithResponseAsync(BaseRequest request, CancellationTokenSource token) {
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

					BaseResponse receivedObj = JsonConvert.DeserializeObject<BaseResponse>(received);

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

		public void Dispose() {
			_ = StopAsync().Result;
			Connector?.Dispose();
			PreviousResponse = null;
			Disconnected = null;
			Connected = null;
			ResponseReceived = null;
			IsReceiving = false;
			ClientSemaphore.Release();
			ClientSemaphore.Dispose();
			ClientReceivingSemaphore.Release();
			ClientReceivingSemaphore.Dispose();
		}
	}
}
