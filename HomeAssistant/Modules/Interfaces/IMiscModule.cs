using System;
using System.Collections.Generic;
using System.Text;

namespace HomeAssistant.Modules.Interfaces {
	public interface IMiscModule :  IModuleBase {

		DateTime ConvertTo24Hours (DateTime source);

		DateTime ConvertTo12Hours (DateTime source);

	}
}
