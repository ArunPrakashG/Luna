using MailKit;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeAssistant.Modules.Interfaces {
	public interface IReceviedMessageDuringIdle {
		IMessageSummary Message { get; set; }

		uint UniqueId { get; set; }

		bool MarkAsRead { get; set; }

		bool MarkedAsDeleted { get; set; }

		DateTime ArrivedTime { get; set; }
	}
}
