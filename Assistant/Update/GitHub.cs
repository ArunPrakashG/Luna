
//    _  _  ___  __  __ ___     _   ___ ___ ___ ___ _____ _   _  _ _____
//   | || |/ _ \|  \/  | __|   /_\ / __/ __|_ _/ __|_   _/_\ | \| |_   _|
//   | __ | (_) | |\/| | _|   / _ \\__ \__ \| |\__ \ | |/ _ \| .` | | |
//   |_||_|\___/|_|  |_|___| /_/ \_\___/___/___|___/ |_/_/ \_\_|\_| |_|
//

//MIT License

//Copyright(c) 2019 Arun Prakash
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using Newtonsoft.Json;
using RestSharp;
using System;
using Assistant.AssistantCore;
using Assistant.Extensions;
using Assistant.Log;

namespace Assistant.Update {

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
		private readonly Logger Logger = new Logger("GIT-HUB");

		public Rootobject FetchLatestAssest(string gitToken) {
			if (Helpers.IsNullOrEmpty(gitToken)) {
				Logger.Log("Token is empty!, cannot proceed.");
				return new Rootobject();
			}

			string json = Helpers.GetUrlToString(Constants.GitHubReleaseURL + "?access_token=" + gitToken, Method.GET, true);

			if (string.IsNullOrEmpty(json) || string.IsNullOrWhiteSpace(json)) {
				Logger.Log("Could not fetch the latest patch release. Try again later!", Enums.LogLevels.Warn);
				return null;
			}

			try {
				Rootobject Root = JsonConvert.DeserializeObject<Rootobject>(json);
				return Root;
			}
			catch (Exception e) {
				Logger.Log(e, Enums.LogLevels.Fatal);
				return null;
			}
		}
	}
}
