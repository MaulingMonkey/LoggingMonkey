namespace LoggingMonkey {
	static class Json {
		public static string ToString(string s) {
			return s == null ? "null" : ("\"" + s.Replace("\\","\\\\").Replace("\"","\\\"") + "\"");
		}
	}
}
