using System.Threading.Tasks;

namespace HomeAssistant.Modules.Interfaces {

	public interface IDiscordLogger {

		/// <summary>
		/// Log to bot discord channel
		/// </summary>
		/// <param name="message">The log message</param>
		/// <returns></returns>
		Task LogToChannel(string message);
	}
}
