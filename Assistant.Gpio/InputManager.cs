using Assistant.Log;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Gpio {
	//TODO: Handle all sensor inputs here
	//Timed relay tasks based on sensor values etc
	public class InputManager {
		private ILogger Logger { get; set; } = new Logger("INPUT-MANAGER");
	}
}
