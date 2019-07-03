using MailKit;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeAssistant.Modules.Interfaces {
	public interface IReceviedMessageDuringIdle {
		/// <summary>
		/// Recevied message data
		/// </summary>
		/// <value></value>
		IMessageSummary Message { get; set; }
		/// <summary>
		/// Message unique id
		/// </summary>
		/// <value></value>
		uint UniqueId { get; set; }
		/// <summary>
		/// Mark the message as read
		/// </summary>
		/// <value></value>
		bool MarkAsRead { get; set; }
		/// <summary>
		/// The message was marked as deleted
		/// </summary>
		/// <value></value>
		bool MarkedAsDeleted { get; set; }
		/// <summary>
		/// DateTime when the message arrived
		/// </summary>
		/// <value></value>
		DateTime ArrivedTime { get; set; }
	}
}
