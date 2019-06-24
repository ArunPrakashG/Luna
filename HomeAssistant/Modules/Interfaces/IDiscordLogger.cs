using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HomeAssistant.Modules.Interfaces {
	public interface IDiscordLogger {

		Task LogToChannel (string message);

	}
}
