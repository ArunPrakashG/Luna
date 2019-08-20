using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.SignalR;

namespace Assistant.Server.SignalR.Hubs {
	public class ChatHub : Hub {
		public void Send(string name, string message) {			
			Clients.All.SendAsync("broadcastMessage", name, message);
		}
	}
}
