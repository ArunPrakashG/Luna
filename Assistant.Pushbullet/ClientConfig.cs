using Assistant.Pushbullet.Exceptions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Assistant.Pushbullet {
	public struct ClientConfig {
		public readonly string AccessToken;
		public readonly IWebProxy Proxy;
		public bool ShouldUseProxy => Proxy != null;

		public ClientConfig(string _accessToken, IWebProxy _proxy) {
			AccessToken = _accessToken ?? throw new IncorrectAccessTokenException(nameof(_accessToken), "Access token cannot be null!");
			Proxy = _proxy;
		}
	}
}
