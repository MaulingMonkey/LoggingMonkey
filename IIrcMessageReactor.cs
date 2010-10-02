using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoggingMonkey {
	interface IIrcMessageReactor {
		bool TryReact( Network network, string message );
	}
}
