using System;
using System.Collections.Generic;
using System.Text;

namespace HomeAssistant.Extensions.Attributes {

	[AttributeUsage(AttributeTargets.All)]
	public class RequireNetworkConnection : Attribute {
	}
}
