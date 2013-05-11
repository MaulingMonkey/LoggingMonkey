using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoggingMonkey {
	class X3MessageServices {
		void TableHeaderSeperator() { }
		void TableFooterSeperator() { }

		CompoundIrcMessageReactor _DefaultReactor = null;
		CompoundIrcMessageReactor DefaultReactor { get {
			if( _DefaultReactor == null )
				_DefaultReactor = CreateDefaultReactor( );
			return _DefaultReactor;
		}}
		CompoundIrcMessageReactor CreateDefaultReactor( ) {
			return new CompoundIrcMessageReactor( ) {
				{ @"^:X3!X3@X3.AfterNET.Services (PRIVMSG|NOTICE) (?<target>[^ ]+) \:?----(-+)$", (network,match) => TableHeaderSeperator( ) },
				{ @"^:X3!X3@X3.AfterNET.Services (PRIVMSG|NOTICE) (?<target>[^ ]+) \:?----(-+)(\s*)End(\s*)(\((?<rows>\d+)\s*Rows\))?.*----$", (network,match) => TableHeaderSeperator( ) },
				};
		}
	}
}
