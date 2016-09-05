using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace LoggingMonkey {
	class LogRequestParameters
	{
		public readonly DateTime From, To;
		public readonly int LinesOfContext;

		public readonly string Network;
		public readonly string Channel;
		public readonly string TimeFmt;

		public readonly bool Cats;
		public readonly bool Tiny;

		public readonly Regex NickQuery;
		public readonly Regex UserQuery;
		public readonly Regex HostQuery;
		public readonly Regex MessQuery;

		public LogRequestParameters(HttpListenerRequest request)
		{
			var vars = request.QueryString;
				
			if (!DateTime.TryParse(vars["from"]   ??"",out From     )) From    = DateTime.Now.AddMinutes(-15);
			if (!DateTime.TryParse(vars["to"]     ??"",out To       )) To      = DateTime.Now.AddMinutes(+15);
			if (!int     .TryParse(vars["context"]??"",out LinesOfContext)) LinesOfContext = 0;
			if ( LinesOfContext <     0 ) LinesOfContext = 0;
			if ( LinesOfContext > 10000 ) LinesOfContext = 10000;

			Network			= vars["server" ]	?? "irc.afternet.org";
			Channel			= vars["channel"]	?? "#gamedev";
			var nickquerys	= vars["nickquery"]	?? null;
			var userquerys	= vars["userquery"]	?? null;
			var hostquerys	= vars["hostquery"]	?? null;
			var querys		= vars["query"]		?? null;
			var querytype	= vars["querytype"]	?? "plaintext";
			TimeFmt			= vars["timefmt"]	?? "pst";

			Func<string,bool> bools = s => new[]{"true","1"}.Contains((vars[s]??"").ToLowerInvariant());
			var casesensitive	= bools("casesensitive");
			Cats				= bools("cats");
			Tiny				= bools("tiny");

			var options
				= RegexOptions.Compiled
				| (casesensitive?RegexOptions.None:RegexOptions.IgnoreCase)
				;

			Func<string,Regex> query_to_regex = input => {
				if ( string.IsNullOrEmpty(input) ) return null;
				switch ( querytype ) {
				case "regex":     return new Regex(input,options);
				case "wildcard":  return new Regex("^"+Regex.Escape(input).Replace(@"\*","(.*)").Replace(@"\?",".")+"$",options);
				case "plaintext": return new Regex(Regex.Escape(input),options);
				default: goto case "plaintext";
				}
			};

			NickQuery = query_to_regex(nickquerys);
			UserQuery = query_to_regex(userquerys);
			HostQuery = query_to_regex(hostquerys);
			MessQuery = query_to_regex(querys    );

			if ( !string.IsNullOrEmpty(nickquerys) && string.IsNullOrEmpty(userquerys) && string.IsNullOrEmpty(hostquerys) )
			{
				Match nuh = Regexps.IrcWhoMask.Match(nickquerys);
				if ( nuh.Success )
				{
					var oldqt = querytype;
					querytype = "wildcard";
					NickQuery = query_to_regex( nuh.Groups["nick"].Value );
					UserQuery = query_to_regex( nuh.Groups["user"].Value );
					HostQuery = query_to_regex( nuh.Groups["host"].Value );
					querytype = oldqt;
				}
			}
		}
	}
}
