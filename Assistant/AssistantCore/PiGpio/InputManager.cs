using Assistant.Log;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.AssistantCore.PiGpio {
	//TODO: Handle all sensor inputs here
	//Timed relay tasks based on sensor values etc
	public class InputManager {
		private Logger Logger { get; set; } = new Logger("INPUT-MANAGER");
		private PiController? Controller => Core.PiController;
	}
}
