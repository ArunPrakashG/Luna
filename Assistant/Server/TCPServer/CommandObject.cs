using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Server.TCPServer {
	public class CommandObject {
		public string ReceviedMessageObject { get; set; } = string.Empty;
		public DateTime ReceviedDateTime { get; set; }

		public CommandObject(string obj, DateTime dt) {
			ReceviedDateTime = dt;

			if (string.IsNullOrEmpty(obj)) {
				return;
			}

			ReceviedMessageObject = obj;
		}

		public override bool Equals(object? obj) {
			CommandObject? obj1 = obj as CommandObject;

			if (obj1 == null) {
				return false;
			}

			if (obj1.ReceviedMessageObject.Equals(ReceviedMessageObject, StringComparison.OrdinalIgnoreCase)) {
				return true;
			}

			return false;
		}

		public override int GetHashCode() {
			return base.GetHashCode();
		}
	}
}
