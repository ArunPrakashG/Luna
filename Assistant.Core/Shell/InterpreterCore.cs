using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System.Threading.Tasks;

namespace Assistant.Core.Shell {
	public class InterpreterCore {
		public enum EXECUTE_RESULT : byte {
			Success = 0x01,
			Failed = 0x00,
			InvalidArgs = 0x002,
			InvalidCommand = 0x003,
			DoesntExist = 0x004
		}
	}
}
