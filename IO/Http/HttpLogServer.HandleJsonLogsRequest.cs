using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace LoggingMonkey {
	partial class HttpLogServer {
		private void HandleJsonLogsRequest( HttpListenerContext context, AccessControlStatus acs, AllLogs logs )
		{
			var vars = context.Request.QueryString;
			DateTime from, to;
			int linesOfContext;
			if (!DateTime.TryParse(vars["from"]   ??"",out from     )) from    = DateTime.Now.AddMinutes(-15);
			if (!DateTime.TryParse(vars["to"]     ??"",out to       )) to      = DateTime.Now.AddMinutes(+15);
			if (!int     .TryParse(vars["context"]??"",out linesOfContext)) linesOfContext = 0;
			if ( linesOfContext <     0 ) linesOfContext = 0;
			if ( linesOfContext > 10000 ) linesOfContext = 10000;

			string network    = vars["server" ]   ?? "irc.afternet.org";
			string channel    = vars["channel"]   ?? "#gamedev";
			string nickquerys = vars["nickquery"] ?? null;
			string userquerys = vars["userquery"] ?? null;
			string hostquerys = vars["hostquery"] ?? null;
			string querys     = vars["query"]     ?? null;
			string querytype  = vars["querytype"] ?? "plaintext";
			string timefmt    = vars["timefmt"]   ?? "pst";

			Func<string,bool> bools = s => new[]{"true","1"}.Contains((vars[s]??"").ToLowerInvariant());
			bool casesensitive = bools("casesensitive");
			bool cats          = bools("cats");
			bool tiny          = bools("tiny");

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

			Regex nickquery = query_to_regex(nickquerys);
			Regex userquery = query_to_regex(userquerys);
			Regex hostquery = query_to_regex(hostquerys);
			Regex query     = query_to_regex(querys    );

			if ( !string.IsNullOrEmpty(nickquerys) && string.IsNullOrEmpty(userquerys) && string.IsNullOrEmpty(hostquerys) )
			{
				Match nuh = Regexps.IrcWhoMask.Match(nickquerys);
				if ( nuh.Success )
				{
					var oldqt = querytype;
					querytype = "wildcard";
					nickquery = query_to_regex( nuh.Groups["nick"].Value );
					userquery = query_to_regex( nuh.Groups["user"].Value );
					hostquery = query_to_regex( nuh.Groups["host"].Value );
					querytype = oldqt;
				}
			}

			using ( var writer = new StreamWriter(context.Response.OutputStream) ) {
				var notices = new List<string>();

				writer.WriteLine(@"{");

				ChannelLogs clog = null;
				if ( logs==null ) {										notices.Add(string.Format("Logs are currently loading.  Reload this page in a minute."));
				} else lock (logs) if ( !logs.ContainsKey(network) ) {	notices.Add(string.Format("Not serving logs for {0}", network));
				} else if ( !logs[network].HasChannel(channel) ) {		notices.Add(string.Format("Not serving logs for {0}", channel));
				} else {
					writer.WriteLine(@"	""channels"": [{0}],", logs == null ? "" : string.Join(",",logs[network].Channels.Select(Json.ToString)));
					clog = logs[network].Channel(channel);
					if ( clog.RequireAuth && !Allow(acs) )
					{
						switch( acs )
						{
						case AccessControlStatus.Admin:			notices.Add(string.Format("Not (yet) authorized to access channel logs for {0}.  You're somehow simultaniously an admin yet not allowed in.", channel )); break;
						case AccessControlStatus.Whitelisted:	notices.Add(string.Format("Not (yet) authorized to access channel logs for {0}.  You're somehow simultaniously whitelisted yet not allowed in.", channel )); break;
						case AccessControlStatus.Pending:		notices.Add(string.Format("Not (yet) authorized to access channel logs for {0}.  Authorization cookie set, whitelisting pending.", channel )); break;
						case AccessControlStatus.Error:			notices.Add(string.Format("Not (yet) authorized to access channel logs for {0}.  PM LoggingMonkey !auth to set an authorization cookie.", channel )); break;
						case AccessControlStatus.Blacklisted:	notices.Add(string.Format("Not (yet) authorized to access channel logs for {0}.  Authorization cookie set, whitelisting pending...", channel )); break;
						}
						clog = null;
					}
					else if( clog.RequireAuth && acs == AccessControlStatus.Error )
					{
						notices.Add(string.Format("NOTICE: LoggingMonkey will soon switch to a whitelist.  You don't currently have an authorization cookie set -- please PM LoggingMonkey !auth for a biodegradable and reusable authorization link.  #gamedev ban-ees need not apply.<"));
					}
				}

				writer.WriteLine(@"	""notices"": [{0}],", string.Join(",",notices.Select(Json.ToString)));
				if( acs == AccessControlStatus.Admin ) writer.WriteLine(@"	""access"": ""admin"",");

				var pst = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
				if( clog==null )
				{
					writer.WriteLine(@"	""chat"": []");
				}
				else
				{
					var start2 = DateTime.Now;
					int linesMatched = 0;
					int linesWritten = 0;
					int linesSearched= 0;

					writer.WriteLine(@"	""chat"": [");

					Action<FastLogReader.Line,List<string>> write = (line, tags) => {
						//bool isTor = Tor.Lines.Contains(line.Host) || DnsCache.ResolveDontWait(line.Host).Any(ipv4=>Tor.Lines.Contains(ipv4));
						if (AccessControl.InTwitlist( line.NUH )) tags.Add("twit");

						var whenS = "";
						switch( timefmt )
						{
						case "longpst": whenS = line.When.ToString("M/d/yy hh:mm:ss tt",Program.Culture); break;
						case "longutc": whenS = TimeZoneInfo.ConvertTimeToUtc( line.When, pst ).ToString("M/d/yy HH:mm:ss",Program.Culture); break;
						case "pst":		whenS = line.When.ToString("M/d h:mm tt",Program.Culture); break;
						default:		whenS = line.When.ToString("M/d h:mm tt",Program.Culture); break;
						}

						writer.Write(linesWritten++ == 0 ? "		" : ",\n		");
						writer.Write("{ \"type\": "+Json.ToString(line.Type.ToString()));
						writer.Write(", \"when\": "+Json.ToString(whenS));
						writer.Write(", \"nick\": "+Json.ToString(line.Nick));
						writer.Write(", \"user\": "+Json.ToString(line.User));
						writer.Write(", \"host\": "+Json.ToString(line.Host));
						if (tags.Count > 0)
							writer.Write(", \"tags\": [{0}]", string.Join(",",tags.Select(Json.ToString)));
						writer.Write(", \"message\": "+Json.ToString(line.Message));
						writer.Write(" }");
					};

					int moreContext = -1;
					Queue<FastLogReader.Line> PreContext = new Queue<FastLogReader.Line>();

					bool highlight_matches = (nickquery!=null || hostquery!=null || query!=null) && linesOfContext>0;

					foreach ( var line in FastLogReader.ReadAllLines(network,channel,from,to) ) {
						bool lineMatch
							=  ( from <= line.When && line.When <= to )
							&& ( nickquery == null || nickquery.IsMatch(line.Nick   ??"") )
							&& ( userquery == null || userquery.IsMatch(line.User   ??"") )
							&& ( hostquery == null || hostquery.IsMatch(line.Host   ??"") )
							&& ( query     == null || query    .IsMatch(line.Message??"") )
							;

						++linesSearched;
						if ( lineMatch ) {
							++linesMatched;
							// write out pre-context and write line
							var nextLineTags = new List<string>();
							if ( linesOfContext!=0 && PreContext.Count>=linesOfContext && moreContext==-1 ) {
								nextLineTags.Add("break");
							}

							while ( PreContext.Count>0 ) {
								write(PreContext.Dequeue(), nextLineTags);
								nextLineTags.Clear();
							}
							nextLineTags.Add("matched");
							write(line, nextLineTags);
							nextLineTags.Clear();
							moreContext = linesOfContext;
						} else if ( moreContext>0 ) { // not a match, but it's post-context
							write(line, new List<string>());
							--moreContext;
						} else { // not a match, not immediate post-context, start feeding back into pre-context
							if ( linesOfContext!=0 && PreContext.Count>=linesOfContext ) {
								PreContext.Dequeue();
								moreContext = -1;
							}
							if ( linesOfContext!=0 ) PreContext.Enqueue(line);
						}
					}
					var stop2 = DateTime.Now;

					if (linesWritten > 0) writer.WriteLine();
					writer.WriteLine("	],"); // end of "chat" array
					writer.WriteLine("	\"stats\": {{ \"matched\": {1}, \"displayed\": {2}, \"searched\": {3}, \"time\": {0} }}", (stop2-start2).TotalSeconds.ToString("N2"), linesMatched, linesWritten, linesSearched );
				}

				writer.WriteLine("}");
			}
		}
	}
}
