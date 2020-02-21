namespace Assistant.Core.Shell.Internal {
	public static class Helpers {
		public static bool AsBool(this string value, out bool? booleanValue) {
			if (string.IsNullOrEmpty(value)) {
				booleanValue = null;
				return false;
			}

			bool? temp;
			switch (value) {
				case "1":
					temp = true;
					break;
				case "0":
					temp = false;
					break;
				default:
					temp = null;
					break;
			}

			bool parseResult = bool.TryParse(value, out bool parsed);

			if (parseResult && parsed == temp) {
				booleanValue = parsed;
				return true;
			}
			else if (parseResult && parsed != temp) {
				booleanValue = parsed;
				return true;
			}
			else if (!parseResult && parsed != temp) {
				booleanValue = temp;
				return true;
			}
			else {
				booleanValue = null;
				return false;
			}
		}
	}
}
