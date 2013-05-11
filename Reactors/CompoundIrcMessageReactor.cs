using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LoggingMonkey {
	class CompoundIrcMessageReactor : List<IIrcMessageReactor> , IIrcMessageReactor {
		public bool TryReact( Network network, string message ) {
			return this.Any( siml => siml.TryReact(network,message) );
		}

		public void Add( string regex, string response ) {
			Add( new IrcMessageLoggerReactor(regex,response) );
		}

		public void Add( string regex, Action<Network,Match> match ) {
			Add( new IrcMessageRegexReactor(regex,match) );
		}
	}
}
