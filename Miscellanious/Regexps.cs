using System.Text.RegularExpressions;

namespace LoggingMonkey
{
	static class Regexps
	{
		const string fWhen        = @"\[(?<when>[^\]]+)\]";
		const string fUrlContinue = @"(?:[^.,;:!?')""\s]|(\S(?=\S|$)))";
		const string fUrlProtocol = @"([-.+a-zA-Z0-9]+?:\/\/"+fUrlContinue+"+)";
		const string fUrlTLD      = @"([^\s]+?\.(?:com|net|org|edu|gov|mil|info|biz)"+fUrlContinue+"*)";
		const string fUrlBLD      = @"((?:www|ftp)\."+fUrlContinue+"+)";

		public static readonly Regex IrcWhoMask  = new Regex(@"(?<nick>[^;! ]+)!(?<user>[^@ ]+)@(?<host>[^&> ]+)", RegexOptions.Compiled); // Nearly identical to IrcWho, but allows * in nicks
		public static readonly Regex IrcWho      = new Regex(@"(?<nick>[^*;! ]+)!(?<user>[^@ ]+)@(?<host>[^&> ]+)", RegexOptions.Compiled);
		public static readonly Regex LogWhen     = new Regex("^"+fWhen, RegexOptions.Compiled);
		public static readonly Regex LogFilename = new Regex(@".*[\\/](?<network>[^-\\/]+)-(?<channel>#[^-\\/]+)-(?<year>\d+)-(?<month>\d+)-(?<day>\d+)\.log",RegexOptions.Compiled);
		public static readonly Regex LogWho      = IrcWho;
		public static readonly Regex UrlProtocol = new Regex("^"+fUrlProtocol,RegexOptions.Compiled);
		public static readonly Regex UrlPatterns = new Regex(@"\b(?:" + fUrlProtocol + "|"  + fUrlTLD + "|"  + fUrlBLD + ")", RegexOptions.Compiled);
	}
}
