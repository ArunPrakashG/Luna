using System.Threading.Tasks;

namespace HomeAssistant.Extensions {

	public static class Compatibility {
#pragma warning disable 1998

		public static class File {

			public static async Task<byte[]> ReadAllBytesAsync(string path) => await System.IO.File.ReadAllBytesAsync(path).ConfigureAwait(false);
		}

#pragma warning restore 1998
	}
}
