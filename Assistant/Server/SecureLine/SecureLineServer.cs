using Assistant.Extensions;
using Assistant.Log;
using Assistant.Server.SecureLine.Responses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Assistant.Server.SecureLine {
	public class ConnectedClient {
		public string IPAddress { get; set; }
	}

	public class SecureLineServer : IDisposable {
		private TcpListener Listener { get; set; }
		private static int ServerPort { get; set; }
		private static IPAddress ListerningAddress { get; set; } = IPAddress.Any;
		private static IPEndPoint EndPoint { get; set; }
		public static bool IsOnline { get; private set; }
		public static bool StopServer { get; set; }
		private readonly Logger Logger = new Logger("SECURE-LINE");

		public static List<ConnectedClient> ConnectedClients = new List<ConnectedClient>();

		public SecureLineServer(IPAddress address, int port) {
			ListerningAddress = address ?? throw new ArgumentNullException(nameof(address));
			ServerPort = port <= 0 ? throw new ArgumentNullException(nameof(port)) : port;
			EndPoint = new IPEndPoint(ListerningAddress, ServerPort);
		}

		public SecureLineServer() {
			throw new NotSupportedException("Initilize server with parameters!");
		}

		public async Task<bool> StartSecureLine(TimeSpan disposingTime) {
			Logger.Log("Starting secure line server...", AssistantCore.Enums.LogLevels.Info);
			Listener = new TcpListener(EndPoint);
			Listener.Start(10);
			IsOnline = true;

			try {
				while (!StopServer && Listener != null) {
					Socket socket = await Listener.AcceptSocketAsync().ConfigureAwait(false);
					byte[] receiveBuffer = new byte[5024];
					int b = socket.Receive(receiveBuffer);
					EndPoint remoteEndpoint = socket.RemoteEndPoint;
					ConnectedClient client = new ConnectedClient() {
						IPAddress = remoteEndpoint.ToString().Split(':')[0].Trim()
					};
					Logger.Log($"Connected client IP => {client.IPAddress}", AssistantCore.Enums.LogLevels.Trace);

					if (!ConnectedClients.Contains(client)) {
						ConnectedClients.Add(client);
					}

					string recevied = Encoding.ASCII.GetString(receiveBuffer, 0, b);
					string response = await OnRecevied(recevied).ConfigureAwait(false);

					if (!Helpers.IsNullOrEmpty(response)) {
						socket.Send(Encoding.ASCII.GetBytes(ObjectToString<SuccessCommand>(new SuccessCommand() {
							ResponseCode = 0x00,
							ResponseMessage = response
						})));
						goto CON_END;
					}

					socket.Send(Encoding.ASCII.GetBytes(ObjectToString<FailedCommand>(new FailedCommand() {
						ResponseCode = 0x01,
						FailReason = "the processed result is null!"
					})));

				CON_END:
					socket.Close();
				}
			}
			catch (Exception e) {
				Logger.Log(e);
			}
			finally {
				Listener.Stop();
			}

			return true;
		}

		private Task<string> OnRecevied(string recevied) {
			return null;
		}

		public void Dispose() { }

		public static string ObjectToString<T>(T obj) => JsonConvert.SerializeObject(obj);
		public static T ObjectFromString<T>(string obj) => JsonConvert.DeserializeObject<T>(obj);
	}
}

