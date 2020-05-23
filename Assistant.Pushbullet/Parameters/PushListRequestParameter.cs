using System;

namespace Assistant.Pushbullet.Parameters {
	public struct PushListRequestParameter {
		public readonly TimeSpan ModifiedAfter;
		public readonly bool ActiveOnly;
		public readonly string? Cursor;
		public readonly int MaxResults;

		public PushListRequestParameter(TimeSpan _modifiedAfter, bool _activeOnly, string? cursor, int _maxResultsPerPage) {
			ModifiedAfter = _modifiedAfter;
			ActiveOnly = _activeOnly;
			Cursor = cursor;
			MaxResults = _maxResultsPerPage;
		}
	}
}
