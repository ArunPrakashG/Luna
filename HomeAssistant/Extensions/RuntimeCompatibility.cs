using System;
using System.Threading.Tasks;

namespace HomeAssistant.Extensions {

	public static class RuntimeCompatibility {

		internal static bool IsRunningOnMono => Type.GetType("Mono.Runtime") != null;

#if NET472
		internal static async Task<WebSocketReceiveResult> ReceiveAsync(this WebSocket webSocket, byte[] buffer, CancellationToken cancellationToken) => await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
		internal static async Task SendAsync(this WebSocket webSocket, byte[] buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) => await webSocket.SendAsync(new ArraySegment<byte>(buffer), messageType, endOfMessage, cancellationToken).ConfigureAwait(false);
		internal static string[] Split(this string text, char separator, StringSplitOptions options = StringSplitOptions.None) => text.Split(new[] { separator }, options);
#endif

#pragma warning disable 1998

		internal static class File {

			internal static async Task AppendAllTextAsync(string path, string contents) =>
#if NET472
				System.IO.File.AppendAllText(path, contents);
#else
				await System.IO.File.AppendAllTextAsync(path, contents).ConfigureAwait(false);

#endif

			internal static async Task<byte[]> ReadAllBytesAsync(string path) =>
#if NET472
				System.IO.File.ReadAllBytes(path);
#else
				await System.IO.File.ReadAllBytesAsync(path).ConfigureAwait(false);

#endif

			internal static async Task<string> ReadAllTextAsync(string path) =>
#if NET472
				System.IO.File.ReadAllText(path);
#else
				await System.IO.File.ReadAllTextAsync(path).ConfigureAwait(false);

#endif

			internal static async Task WriteAllTextAsync(string path, string contents) =>
#if NET472
				System.IO.File.WriteAllText(path, contents);
#else
				await System.IO.File.WriteAllTextAsync(path, contents).ConfigureAwait(false);

#endif
		}

#pragma warning restore 1998

		internal static class Path {

			internal static string GetRelativePath(string relativeTo, string path) {
#if NET472

				// This is a very silly implementation
				if (!path.StartsWith(relativeTo, StringComparison.OrdinalIgnoreCase)) {
					throw new NotImplementedException();
				}

				string result = path.Substring(relativeTo.Length);
				return (result[0] == System.IO.Path.DirectorySeparatorChar) || (result[0] == System.IO.Path.AltDirectorySeparatorChar) ? result.Substring(1) : result;
#else
#pragma warning disable IDE0022
				return System.IO.Path.GetRelativePath(relativeTo, path);
#pragma warning restore IDE0022
#endif
			}
		}
	}
}
