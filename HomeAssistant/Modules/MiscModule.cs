using HomeAssistant.Log;
using System;

namespace HomeAssistant.Modules {
	public class MiscModule {
		private Logger Logger = new Logger("MISC-MODULE");

		public MiscModule() {

		}

		public DateTime ConvertTo24Hours(DateTime source) {
			bool sucess = DateTime.TryParse(source.ToString("yyyy MMMM d HH:mm:ss tt"), out DateTime result);
			if (sucess) {
				return result;
			}
			else {
				return DateTime.Now;
			}
		}

		public DateTime ConvertTo12Hours(DateTime source) {
			bool sucess = DateTime.TryParse(source.ToString("dddd, dd MMMM yyyy"), out DateTime result);
			if (sucess) {
				return result;
			}
			else {
				return DateTime.Now;
			}
		}
	}
}
