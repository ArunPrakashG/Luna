using System;

namespace Assistant.PushBullet.Exceptions {
	public class IncorrectAccessTokenException : Exception {
		public IncorrectAccessTokenException() : base("Empty or Incorrect api key specified.") {
		}

		public IncorrectAccessTokenException(string apiKey) : base($"Empty or Incorrect api key specified. ({apiKey})") {
		}

		public IncorrectAccessTokenException(string apiKey, string message) : base($"{message} ({apiKey})") {
		}
	}
}
