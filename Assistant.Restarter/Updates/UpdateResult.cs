namespace Luna.External.Updates {
	internal class UpdateResult {
		internal bool IsUpdateAvailable;
		internal readonly GithubResponse Response;

		internal UpdateResult(GithubResponse response, bool isUpdateAvailable = false) {
			Response = response ?? new GithubResponse();
		}
	}
}
