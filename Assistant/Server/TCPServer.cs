using HomeAssistant.Core;
using HomeAssistant.Extensions;
using HomeAssistant.Log;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using HomeAssistant.Extensions.Attributes;
using Unosquare.RaspberryIO.Abstractions;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Server {

	[Obsolete("Use Kestrel server instead of TCP server.")]
	public class TCPServer : Tess {
		private readonly Logger Logger;
		private readonly byte[] Buffer = new byte[1024];
		private string ReceivedData;
		private Socket Sock;
		public bool ServerOn;

		public TCPServer() {
			Logger = new Logger("SERVER");
		}

		public void StopServer() {
			if (Sock == null) {
				return;
			}

			Logger.Log("Stopping Server...");
			Sock.Close(2);
			Task.Delay(300).Wait();
			Sock.Dispose();
			ServerOn = false;
			Logger.Log("TCP Server stopped and disposed!");
		}

		public bool StartServer() {
			try {
				IPEndPoint localEndpoint = new IPEndPoint(IPAddress.Any, Config.TCPServerPort);
				string externalip = new WebClient().DownloadString("https://api.ipify.org/").Trim('\n');
				Logger.Log("Public ip fetched sucessfully => " + externalip, LogLevels.Trace);
				Logger.Log("Local ip => " + Helpers.GetLocalIpAddress(), LogLevels.Trace);
				Logger.Log("Starting assistant command server...", LogLevels.Trace);
				Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				Logger.Log("Server started sucessfully on address: " + externalip + ":" + Config.TCPServerPort);

				Sock.Bind(localEndpoint);
				Sock.Listen(15);
				ServerOn = true;
				Logger.Log("Listerning for connections...");
				int errorCounter = 0;

				Helpers.InBackgroundThread(() => {
					while (true) {
						try {
							if (Sock != null)
							{
								Socket Socket = Sock?.Accept();
								int b = Socket.Receive(Buffer);
								EndPoint ep = Socket.RemoteEndPoint;
								string[] IpData = ep.ToString().Split(':');
								string clientIP = IpData[0].Trim();
								Logger.Log("Client IP: " + clientIP, LogLevels.Trace);

								if (File.Exists(Constants.IPBlacklistPath) && File.ReadAllLines(Constants.IPBlacklistPath).Any(x => x.Equals(clientIP))) {
									Logger.Log("Unauthorized IP Connected: " + clientIP, LogLevels.Error);
									Socket.Close();
								}

								ReceivedData = Encoding.ASCII.GetString(Buffer, 0, b);
								Logger.Log($"Client Connected > {clientIP} > {ReceivedData}", LogLevels.Trace);
								string resultText = OnRecevied(ReceivedData).Result;

								if (resultText != null) {
									byte[] Message = Encoding.ASCII.GetBytes(resultText);
									Socket.Send(Message);
								}
								else {
									byte[] Message = Encoding.ASCII.GetBytes("Bad Command!");
									Socket.Send(Message);
								}

								Socket.Close();
							}

							Logger.Log("Client Disconnected.", LogLevels.Trace);

							if (!ServerOn) {
								Helpers.ScheduleTask(() => {
									StopServer();
									StartServer();
								}, TimeSpan.FromMinutes(1));
								Logger.Log("Restarting server in 1 minute as it went offline.");
								return;
							}
						}
						catch (SocketException se) {
							Logger.Log(se.Message, LogLevels.Trace);
							errorCounter++;

							if (errorCounter > 5) {
								Logger.Log("too many errors occured on TCP Server. stopping...", LogLevels.Error);
								StopServer();
							}

							if (Sock != null) {
								StopServer();
								if (!ServerOn) {
									Helpers.ScheduleTask(() => {
										StopServer();
										StartServer();
									}, TimeSpan.FromMinutes(1));
									Logger.Log("Restarting server in 1 minute as it went offline.");
									return;
								}
							}
						}
						catch (InvalidOperationException ioe) {
							Logger.Log(ioe.Message, LogLevels.Error);
							errorCounter++;
							if(errorCounter > 5) {
								Logger.Log("too many errors occured on TCP Server. stopping...", LogLevels.Error);
								StopServer();
							}
						}
					}
				}, "TCP Server", true);
			}
			catch (IOException io) {
				Logger.Log(io.Message, LogLevels.Error);
			}
			catch (FormatException fe) {
				Logger.Log(fe.Message, LogLevels.Error);
			}
			catch (IndexOutOfRangeException ioore) {
				Logger.Log(ioore.Message, LogLevels.Error);
			}
			catch (SocketException se) {
				Logger.Log(se.Message, LogLevels.Error);
				Helpers.ScheduleTask(() => {
					StartServer();
				}, TimeSpan.FromMinutes(1));
				Logger.Log("Restarting server in 1 minute as it went offline.");
			}
			return true;
		}

		// Command Format
		// AUTH_CODE|CONTEXT|STATE|VALUE1|VALUE2|VALUE3
		// 3033|GPIO|OUTPUT|2 (pin number)|LOW (pin voltage)
		// 3033|FETCH|2 (pin number) => OK|IsOn status|Pin Number|Mode status
		private async Task<string> OnRecevied(string datarecevied) {
			if (string.IsNullOrEmpty(datarecevied) || string.IsNullOrWhiteSpace(datarecevied)) {
				Logger.Log("Data is null!", LogLevels.Error);
				return "Failed!";
			}

			if (!VerifyAuthentication(datarecevied)) {
				Logger.Log("Incorrect Authentication code. cannot proceed...", LogLevels.Error);
				return "Failed!";
			}

			PiContext PiContext = PiContext.GPIO;
			PiState PiState = PiState.OUTPUT;
			PiPinNumber PinNumber = PiPinNumber.PIN_2;
			PiVoltage PinVoltage = PiVoltage.LOW;

			string[] rawData = datarecevied.Split('|');

			switch (rawData[1].Trim()) {
				case "GPIO":
					PiContext = PiContext.GPIO;
					switch (rawData[2].Trim()) {
						case "OUTPUT":
							PiState = PiState.OUTPUT;

							switch (rawData[4].Trim()) {
								case "HIGH":
									PinVoltage = PiVoltage.HIGH;

									try {
										PinNumber = (PiPinNumber) Convert.ToInt32(rawData[3].Trim());
									}
									catch (Exception) {
										Logger.Log("Pin number doesnt satisfy.", LogLevels.Warn);
										return "Pin number doesnt satisfy.";
									}

									break;

								case "LOW":
									PinVoltage = PiVoltage.LOW;

									try {
										PinNumber = (PiPinNumber) Convert.ToInt32(rawData[3].Trim());
									}
									catch (Exception) {
										Logger.Log("Pin number doesnt satisfy.", LogLevels.Warn);
										return "Pin number doesnt satisfy.";
									}

									break;

								default:
									Logger.Log("Voltage doesnt satisfy.", LogLevels.Warn);
									return "Voltage doesnt satisfy.";
							}

							break;

						case "INPUT":
							PiState = PiState.INPUT;

							switch (rawData[4].Trim()) {
								case "HIGH":
									PinVoltage = PiVoltage.HIGH;

									try {
										PinNumber = (PiPinNumber) Convert.ToInt32(rawData[3].Trim());
									}
									catch (Exception) {
										Logger.Log("Pin number doesnt satisfy.", LogLevels.Warn);
										return "Pin number doesnt satisfy.";
									}

									break;

								case "LOW":
									PinVoltage = PiVoltage.LOW;

									try {
										PinNumber = (PiPinNumber) Convert.ToInt32(rawData[3].Trim());
									}
									catch (Exception) {
										Logger.Log("Pin number doesnt satisfy.", LogLevels.Warn);
										return "Pin number doesnt satisfy.";
									}

									break;

								default:
									Logger.Log("Voltage doesnt satisfy.", LogLevels.Warn);
									return "Voltage doesnt satisfy.";
							}

							break;

						default:
							Logger.Log("States doesnt satisfy.", LogLevels.Warn);
							return "States doesnt satisfy.";
					}
					break;

				case "FETCH":
					PiContext = PiContext.FETCH;
					int pinNumber = Convert.ToInt32(rawData[2].Trim());
					GPIOPinConfig status = Controller.FetchPinStatus(pinNumber);
					if (status == null) {
						Logger.Log("Fetch failed.");
						return $"FAILED|FETCH_FAIL";
					}
					else {
						Logger.Log("Fetch command sucess!");
						return $"OK|{status.IsOn.ToString()}|{pinNumber.ToString()}|{status.Mode.ToString()}";
					}
				case "SHUTDOWN":
					PiContext = PiContext.SHUTDOWN;
					switch (rawData[2].Trim()) {
						case "TESS":
							Helpers.InBackground(async () => {
								Task.Delay(2000).Wait();
								await Exit().ConfigureAwait(false);
							});
							return "Exiting TESS in 2 seconds...";

						case "PI":
							Helpers.InBackground(() => {
								Task.Delay(2000).Wait();
								Helpers.ExecuteCommand("sudo shutdown -h now");
							});
							return "Rasperry Pi shutting down in 2 seconds...";
					}
					break;

				case "RESTART":
					PiContext = PiContext.RESTART;
					switch (rawData[2].Trim()) {
						case "TESS":
							Helpers.InBackground(async () => {
								Task.Delay(2000).Wait();
								await Restart().ConfigureAwait(false);
							});
							return "Restarting TESS in 2 seconds...";
					}
					break;

				case "RELAY":
					PiContext = PiContext.RELAY;
					switch (Convert.ToInt32(rawData[2].Trim())) {
						case (int) GPIOCycles.Cycle:
							Helpers.InBackgroundThread(async () => await Controller.RelayTestService(GPIOCycles.Cycle).ConfigureAwait(false), "Relay Cycle");
							return "Cycle completed!";

						case (int) GPIOCycles.OneMany:
							Helpers.InBackgroundThread(async () => await Controller.RelayTestService(GPIOCycles.OneMany).ConfigureAwait(false), "Relay Cycle");
							return "OneMany cycle completed!";

						case (int) GPIOCycles.OneOne:
							Helpers.InBackgroundThread(async () => await Controller.RelayTestService(GPIOCycles.OneOne).ConfigureAwait(false), "Relay Cycle");
							return "OneOne cycle comepleted!";

						case (int) GPIOCycles.OneTwo:
							Helpers.InBackgroundThread(async () => await Controller.RelayTestService(GPIOCycles.OneTwo).ConfigureAwait(false), "Relay Cycle");
							return "OneTwo cycle completed!";

						case (int) GPIOCycles.Base:
							Helpers.InBackgroundThread(async () => await Controller.RelayTestService(GPIOCycles.Base).ConfigureAwait(false), "Relay Cycle");
							return "Base cycle completed!";

						case (int) GPIOCycles.Single:
							Helpers.InBackgroundThread(async () => await Controller.RelayTestService(GPIOCycles.Single, Convert.ToInt32(rawData[3].Trim())).ConfigureAwait(false), "Relay Cycle");
							return $"Sucessfully configured {rawData[3].Trim()} pin";

						case (int) GPIOCycles.Default:
							Helpers.InBackgroundThread(async () => await Controller.RelayTestService(GPIOCycles.Default).ConfigureAwait(false), "Relay Cycle");
							return "Sucessfully completed default cycle!";
					}
					break;

				default:
					Logger.Log("Context doesnt satisfy.", LogLevels.Warn);
					return "Failed context!";
			}

			bool Status = await ProcessCommand(PiContext, PiState, PinNumber, PinVoltage).ConfigureAwait(false);

			if (Status) {
				return $"OK|Sucessfully configured GPIO PIN {(int) PinNumber}";
			}
			else {
				return $"FAIL|Unable to configure.";
			}
		}

		private async Task<bool> ProcessCommand(PiContext context, PiState state, PiPinNumber pinNumber, PiVoltage voltage) {
			bool Result = false;
			switch (context) {
				case PiContext.GPIO:
					Result = Controller.SetGPIO((int) pinNumber, (GpioPinDriveMode) state, voltage == PiVoltage.HIGH ? GpioPinValue.High : GpioPinValue.Low);
					await System.Threading.Tasks.Task.Delay(300).ConfigureAwait(false);
					return Result;

				default:
					Logger.Log("Context doesnt satisfy. (ProcessCommand())", LogLevels.Warn);
					await System.Threading.Tasks.Task.Delay(300).ConfigureAwait(false);
					return Result;
			}
		}

		private bool VerifyAuthentication(string dataRecevied) {
			if (string.IsNullOrEmpty(dataRecevied) || string.IsNullOrWhiteSpace(dataRecevied)) {
				Logger.Log("Data is null!", LogLevels.Error);
				return false;
			}

			int Code = Convert.ToInt32(dataRecevied.Split('|')[0]);

			if (Code.Equals(Config.ServerAuthCode)) {
				return true;
			}
			else {
				Logger.Log("Incorrect or unknown Auth Code.", LogLevels.Error);
				return false;
			}
		}
	}
}
