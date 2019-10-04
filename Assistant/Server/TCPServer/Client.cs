using Assistant.Extensions;
using Assistant.Log;
using Org.BouncyCastle.Utilities.Net;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Assistant.AssistantCore.Enums;

namespace Assistant.Server.TCPServer {
	public class Payload {
		public string Message { get; set; } = string.Empty;
		public DateTime ReceviedTime { get; set; }
	}

	public class Client {
		public string? UniqueId { get; private set; }
		public string? IpAddress { get; set; }
		public Socket ClientSocket { get; set; }
		public bool DisconnectClient { get; set; }
		public EndPoint? ClientEndPoint { get; set; }
		private readonly Logger Logger = new Logger("CONN-CLIENT");
		private (int?, Thread?) RecevieThread;

		public Client(Socket sock) {
			ClientSocket = sock ?? throw new ArgumentNullException(nameof(sock), "Socket cannot be null!");
			ClientEndPoint = ClientSocket.RemoteEndPoint;
			IpAddress = ClientEndPoint?.ToString()?.Split(':')[0].Trim();

			if(IpAddress != null && !IpAddress.IsNull()) {
				UniqueId = GenerateUniqueId(IpAddress);
			}
			else {
				UniqueId = GenerateUniqueId(ClientSocket.GetHashCode().ToString());
			}
		}

		public void Init() {
			Logger.Log($"Connected client IP => {IpAddress} / {UniqueId}", LogLevels.Info);
			RecevieLoop();
		}

		public void DisconnectConnection() {
			DisconnectClient = true;

			if (RecevieThread.Item2 != null && RecevieThread.Item2.ThreadState != ThreadState.Stopped) {
				RecevieThread.Item2.Abort();
			}

			if (TCPServerCore.Clients.Contains(this)) {
				TCPServerCore.Clients.Remove(this);
				$"Disconnected and disposed client -> {UniqueId} / {IpAddress}".LogInfo(Logger);
				return;
			}

			$"Disconnected client -> {UniqueId} / {IpAddress}".LogInfo(Logger);
		}

		private void RecevieLoop() {
			byte[] receiveBuffer = new byte[5024];
			RecevieThread = Helpers.InBackgroundThread(async () => {
				while (!DisconnectClient && ClientSocket.Connected) {
					try {
						int i = ClientSocket.Receive(receiveBuffer);
						if (i == -1) {
							break;
						}

						string receviedMessage = Encoding.ASCII.GetString(receiveBuffer);

						if (Helpers.IsNullOrEmpty(receviedMessage)) {
							await Task.Delay(1).ConfigureAwait(false);
							continue;
						}

						Payload payload = new Payload() {
							Message = receviedMessage,
							ReceviedTime = DateTime.Now
						};

						OnRecevied(payload);
						await Task.Delay(1).ConfigureAwait(false);
					}
					catch (SocketException se) {
						Logger.Log(se);
						//this means client was forcefully disconnected from the remote endpoint. so process it here and disconnect the client and dispose
						DisconnectConnection();
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

				if (ClientSocket != null) {
					ClientSocket.Close();
					ClientSocket.Dispose();
				}

			}, UniqueId ?? throw new ArgumentNullException("The unique id of the client cannot be null."), true);
		}

		private void OnRecevied(Payload payload) {
			if (payload == null || Helpers.IsNullOrEmpty(payload.Message)) {
				return;
			}

			$"Recevied message -> {UniqueId} -> {payload.Message}".LogInfo(Logger);

			//TODO process the message and send the reply
			//TODO: just send the recevied message back for now
			OnProcessedResult(payload.Message);
		}

		private void OnProcessedResult(string response) {
			if (Helpers.IsNullOrEmpty(response)) {
				return;
			}

			if (!Helpers.IsSocketConnected(ClientSocket)) {
				Logger.Log("Failed to send response as client is disconnected.", LogLevels.Warn);
				DisconnectConnection();
				return;
			}

			ClientSocket.Send(Encoding.ASCII.GetBytes(response));
		}

		public static string GenerateUniqueId(string ipAddress) {
			if (Helpers.IsNullOrEmpty(ipAddress)) {
				return string.Empty;
			}

			return ipAddress.ToLowerInvariant().Trim().GetHashCode().ToString();
		}
	}
}
