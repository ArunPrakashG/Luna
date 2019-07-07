using HomeAssistant.Log;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HomeAssistant.AssistantCore;

namespace HomeAssistant.Server {

	public static class WebUtilities {
		private static readonly Logger Logger = new Logger("WEB-UTILITIES");

		internal static async Task Generate(this HttpResponse httpResponse, HttpStatusCode statusCode) {
			if (httpResponse == null) {
				Logger.Log(nameof(httpResponse), Enums.LogLevels.Error);

				return;
			}

			ushort statusCodeNumber = (ushort) statusCode;

			httpResponse.StatusCode = statusCodeNumber;
			await httpResponse.WriteAsync(statusCodeNumber + " - " + statusCode).ConfigureAwait(false);
		}

		internal static string GetUnifiedName(this Type type) {
			if (type == null) {
				Logger.Log(nameof(type), Enums.LogLevels.Error);

				return null;
			}

			return type.GenericTypeArguments.Length == 0 ? type.FullName : type.Namespace + "." + type.Name + string.Join("", type.GenericTypeArguments.Select(innerType => '[' + innerType.GetUnifiedName() + ']'));
		}

		internal static Type ParseType(string typeText) {
			if (string.IsNullOrEmpty(typeText)) {
				Logger.Log(nameof(typeText), Enums.LogLevels.Error);

				return null;
			}

			Type targetType = Type.GetType(typeText);

			if (targetType != null) {
				return targetType;
			}

			// We can try one more time by trying to smartly guess the assembly name from the namespace, this will work for custom libraries like SteamKit2
			int index = typeText.IndexOf('.');

			if ((index <= 0) || (index >= typeText.Length - 1)) {
				return null;
			}

			return Type.GetType(typeText + "," + typeText.Substring(0, index));
		}
	}
}
