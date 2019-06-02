using HomeAssistant.Extensions;
using HomeAssistant.Log;
using Newtonsoft.Json;
using RestSharp;
using System;
using static HomeAssistant.Core.Enums;

namespace HomeAssistant.Update {

	public class Rootobject {
		public string url { get; set; }
		public string assets_url { get; set; }
		public string upload_url { get; set; }
		public string html_url { get; set; }
		public int id { get; set; }
		public string node_id { get; set; }
		public string tag_name { get; set; }
		public string target_commitish { get; set; }
		public string name { get; set; }
		public bool draft { get; set; }
		public Author author { get; set; }
		public bool prerelease { get; set; }
		public DateTime created_at { get; set; }
		public DateTime published_at { get; set; }
		public Asset[] assets { get; set; }
		public string tarball_url { get; set; }
		public string zipball_url { get; set; }
		public string body { get; set; }
	}

	public class Author {
		public string login { get; set; }
		public int id { get; set; }
		public string node_id { get; set; }
		public string avatar_url { get; set; }
		public string gravatar_id { get; set; }
		public string url { get; set; }
		public string html_url { get; set; }
		public string followers_url { get; set; }
		public string following_url { get; set; }
		public string gists_url { get; set; }
		public string starred_url { get; set; }
		public string subscriptions_url { get; set; }
		public string organizations_url { get; set; }
		public string repos_url { get; set; }
		public string events_url { get; set; }
		public string received_events_url { get; set; }
		public string type { get; set; }
		public bool site_admin { get; set; }
	}

	public class Asset {
		public string url { get; set; }
		public int id { get; set; }
		public string node_id { get; set; }
		public string name { get; set; }
		public object label { get; set; }
		public Uploader uploader { get; set; }
		public string content_type { get; set; }
		public string state { get; set; }
		public int size { get; set; }
		public int download_count { get; set; }
		public DateTime created_at { get; set; }
		public DateTime updated_at { get; set; }
		public string browser_download_url { get; set; }
	}

	public class Uploader {
		public string login { get; set; }
		public int id { get; set; }
		public string node_id { get; set; }
		public string avatar_url { get; set; }
		public string gravatar_id { get; set; }
		public string url { get; set; }
		public string html_url { get; set; }
		public string followers_url { get; set; }
		public string following_url { get; set; }
		public string gists_url { get; set; }
		public string starred_url { get; set; }
		public string subscriptions_url { get; set; }
		public string organizations_url { get; set; }
		public string repos_url { get; set; }
		public string events_url { get; set; }
		public string received_events_url { get; set; }
		public string type { get; set; }
		public bool site_admin { get; set; }
	}

	internal class GitHub {
		private Logger Logger = new Logger("GIT-HUB");

		public Rootobject FetchLatestAssest(string gitToken) {
			if (string.IsNullOrEmpty(gitToken) || string.IsNullOrWhiteSpace(gitToken)) {
				Logger.Log("Token is empty!, cannot proceed.");
				return new Rootobject();
			}

			string json = Helpers.GetUrlToString(Constants.GitHubReleaseURL + "?access_token=" + gitToken, Method.GET, true);

			if (string.IsNullOrEmpty(json) || string.IsNullOrWhiteSpace(json)) {
				Logger.Log("Could not fetch the latest patch release. Try again later!", LogLevels.Warn);
				return null;
			}

			try {
				Rootobject Root = JsonConvert.DeserializeObject<Rootobject>(json);
				return Root;
			}
			catch (Exception e) {
				Logger.Log(e, ExceptionLogLevels.Fatal);
				return null;
			}
		}
	}
}
