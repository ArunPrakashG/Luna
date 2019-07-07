using System;

namespace HomeAssistant.Extensions.Attributes {

	[AttributeUsage(AttributeTargets.All)]
	public class RequireNetworkConnection : Attribute {
	}
}
