using Newtonsoft.Json;
using System;

namespace Assistant.Server.TCPServer.Commands {
	public class CommandBase {
		[JsonProperty]
		public DateTime CommandTime { get; set; }

		[JsonProperty]
		public CommandEnums.Command Command { get; set; }

		[JsonProperty]
		public CommandEnums.CommandType CommandType { get; set; }

		[JsonProperty]
		public string CommandParametersJson { get; set; } = string.Empty;

		[JsonIgnore]
		public object? CommandParametersObject => GetCommandParameterObject();

		public string AsJson() => JsonConvert.SerializeObject(this);

		private object? GetCommandParameterObject() {
			switch (Command) {
				case CommandEnums.Command.GetGpio:
					return JsonConvert.DeserializeObject<GetGpio>(CommandParametersJson);
				case CommandEnums.Command.GetWeather:
					return JsonConvert.DeserializeObject<GetWeather>(CommandParametersJson);
				case CommandEnums.Command.SetAlarm:
					return JsonConvert.DeserializeObject<SetAlarm>(CommandParametersJson);
				case CommandEnums.Command.SetGpioDelayed:
					return JsonConvert.DeserializeObject<SetGpioDelayed>(CommandParametersJson);
				case CommandEnums.Command.SetGpioGeneral:
					return JsonConvert.DeserializeObject<SetGpio>(CommandParametersJson);
				case CommandEnums.Command.SetRemainder:
					return JsonConvert.DeserializeObject<SetRemainder>(CommandParametersJson);
				case CommandEnums.Command.InvalidCommand:
					break;
				case CommandEnums.Command.Disconnect:
					break;
				case CommandEnums.Command.Initiate:
					break;
				case CommandEnums.Command.GetOutputPins:
					break;
				case CommandEnums.Command.GetInputPins:
					break;
				default:
					return null;
			}

			return null;
		}
	}
}
