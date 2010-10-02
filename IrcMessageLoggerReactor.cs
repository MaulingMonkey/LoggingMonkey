using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace LoggingMonkey {
	class IrcMessageLoggerReactor : IIrcMessageReactor {
		Regex  Regex;
		string OutputFormat;
		bool   MultipleChannels;

		public IrcMessageLoggerReactor( string regex, string outputformat ) {
			Regex = new Regex(regex,RegexOptions.Compiled);
			OutputFormat = outputformat;
			MultipleChannels = regex.Contains("(?<channels>");
			//Debug.Assert( MultipleChannels || regex.Contains("(?<channel>") ); // XXX -- oh god I hate IRC
		}

		public bool TryReact( Network network, string message ) {
			var m = Regex.Match(message);
			if (!m.Success) return false;

			if ( MultipleChannels )
			foreach ( var channel in m.Groups["channels"].Value.Split( new[]{','} ).Where( ch=>network.Channels.Contains(ch) ) )
			{
				network.Logs.Channel(channel).Log(m,OutputFormat);
			}
			else // !MultipleChannels
			{
				var channel = m.Groups["channel"].Value;
				if ( !network.Channels.Contains(channel) ) return false;
				network.Logs.Channel(channel).Log(m,OutputFormat);
			}

			return true;
		}
	}
}
