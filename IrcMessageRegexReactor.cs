using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace LoggingMonkey {
	class IrcMessageRegexReactor : IIrcMessageReactor {
		Regex                 Regex;
		Action<Network,Match> Reaction;

		public IrcMessageRegexReactor( string regex, Action<Network,Match> reaction ) {
			Regex = new Regex(regex,RegexOptions.Compiled);
			Reaction = reaction;
		}

		public bool TryReact( Network network, string message ) {
			var m = Regex.Match(message);
			if (!m.Success) return false;

			Reaction( network, m );
			return true;
		}
	}
}
