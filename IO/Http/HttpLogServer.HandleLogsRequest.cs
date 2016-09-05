using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace LoggingMonkey {
	partial class HttpLogServer {
		static readonly string IndexHtmlTemplate = Assets.ResourceManager.GetText("index");
		private void HandleLogsRequest( HttpListenerContext context, AccessControlStatus acs, AllLogs logs )
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
				var text = IndexHtmlTemplate
					.Replace("{{Network}}",	network)
					.Replace("{{Channel}}",	channel)
					.Replace("{{FromDate}}",from.ToString(Program.Culture))
					.Replace("{{ToDate}}",	to	.ToString(Program.Culture));

				var iNotices = text.IndexOf("<!-- {{Notices}} -->");
				var iChatLogs = text.IndexOf("<!-- {{ChatLogs}} -->");
				var stops = new[] {iNotices, iChatLogs, text.Length}.Where(i => i != -1).Distinct().OrderByDescending(i=>i).ToList();

				var prevStop = 0;
				while (stops.Count > 0) {
					// pop stop
					var thisStop = stops.Last();
					stops.RemoveAt(stops.Count-1);

					// write text fragment
					writer.Write(text.Substring(prevStop, thisStop-prevStop));
					prevStop = thisStop;

					if (thisStop == iNotices) {
						if ( logs==null ) {										writer.WriteLine( "<div class=\"notice\">Logs are currently loading.  Reload this page in a minute.</div>");
						} else lock (logs) if ( !logs.ContainsKey(network) ) {	writer.WriteLine( "<div class=\"notice\">Not serving logs for {0}</div>", network );
						} else if ( !logs[network].HasChannel(channel) ) {		writer.WriteLine( "<div class=\"notice\">Not serving logs for {0}</div>", channel );
						} else {
							var clog = logs[network].Channel(channel);
							if ( clog.RequireAuth )
							{
								if ( !Allow(acs) )
								{
									switch( acs )
									{
									case AccessControlStatus.Admin:			writer.WriteLine( "<div class=\"notice\">Not (yet) authorized to access channel logs for {0}.  You're somehow simultaniously an admin yet not allowed in.</div>", channel ); break;
									case AccessControlStatus.Whitelisted:	writer.WriteLine( "<div class=\"notice\">Not (yet) authorized to access channel logs for {0}.  You're somehow simultaniously whitelisted yet not allowed in.</div>", channel ); break;
									case AccessControlStatus.Pending:		writer.WriteLine( "<div class=\"notice\">Not (yet) authorized to access channel logs for {0}.  Authorization cookie set, whitelisting pending.</div>", channel ); break;
									case AccessControlStatus.Error:			writer.WriteLine( "<div class=\"notice\">Not (yet) authorized to access channel logs for {0}.  PM LoggingMonkey !auth to set an authorization cookie.</div>", channel ); break;
									case AccessControlStatus.Blacklisted:	writer.WriteLine( "<div class=\"notice\">Not (yet) authorized to access channel logs for {0}.  Authorization cookie set, whitelisting pending...</div>", channel ); break;
									}
									clog = null;
								}
								else
								{
									writer.WriteLine("<div class=\"notice\">NOTICE: LoggingMonkey will soon switch to a whitelist.  You don't currently have an authorization cookie set -- please PM LoggingMonkey !auth for a biodegradable and reusable authorization link.  #gamedev ban-ees need not apply.</div>");
								}
							}
						}
					} else if (thisStop == iChatLogs) {
						var clog = logs[network].Channel(channel);
						if (!clog.RequireAuth || Allow(acs))
						{
							var pst = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
							if( acs == AccessControlStatus.Admin )
							{
								writer.WriteLine("<hr>");
								writer.WriteLine("<div style=\"background: lightgreen;\">");
								writer.WriteLine("Hello admin!  You now have access to the following channel commands:");
								writer.WriteLine("	!whitelist nick!user@host");
								writer.WriteLine("	!blacklist nick!user@host");
								writer.WriteLine("	!twit[list] nick!user@host");
								writer.WriteLine("	!untwit[list] nick!user@host");
								writer.WriteLine("</div>");
							}

							var start2 = DateTime.Now;
							int linesMatched = 0;
							int linesWritten = 0;
							int linesSearched= 0;

							Action<FastLogReader.Line> write_nuh = (line) => {
								bool isTor = false; //Tor.Lines.Contains(line.Host) || DnsCache.ResolveDontWait(line.Host).Any(ipv4=>Tor.Lines.Contains(ipv4));
								bool isTwit = AccessControl.InTwitlist( line.NUH );
								bool showCrossbones = isTor; // || isTwit;

								string class_ = isTor ? "tor" : isTwit ? "twit" : "normal";
								string title = line.NUH + (isTor ? " !TOR!" : isTwit ? " !TWIT!" : "");
								string nickFormat = showCrossbones ? "&#9760; {0} &#9760;" : "{0}";

								writer.Write("<a class='{0}' title='{1}'>",class_,HttpUtility.HtmlEncode(title));
								writer.Write( nickFormat, HttpUtility.HtmlEncode( line.Nick ) );
								writer.Write("</a>");
							};

							Action<FastLogReader.Line> write = (line) => {
								++linesWritten;

								writer.Write("[");
								switch( timefmt )
								{
								case "longpst": writer.Write(line.When.ToString("M/d/yy hh:mm:ss tt",Program.Culture)); break;
								case "longutc": writer.Write(TimeZoneInfo.ConvertTimeToUtc( line.When, pst ).ToString("M/d/yy HH:mm:ss",Program.Culture)); break;
								case "pst":		writer.Write(line.When.ToString("M/d h:mm tt",Program.Culture)); break;
								default:		writer.Write(line.When.ToString("M/d h:mm tt",Program.Culture)); break;
								}
								writer.Write("] ");

								switch ( line.Type ) {
								case FastLogReader.LineType.Action:		writer.Write("*");		write_nuh(line);								writer.Write(" ");		writer.Write(Program.HtmlizeUrls(HttpUtility.HtmlEncode(line.Message),cats));	writer.Write("*<br>\n"); break;
								case FastLogReader.LineType.Message:	writer.Write("&lt;");	write_nuh(line);								writer.Write("&gt; ");	writer.Write(Program.HtmlizeUrls(HttpUtility.HtmlEncode(line.Message),cats));	writer.Write("<br>\n"); break;
								case FastLogReader.LineType.Join:		writer.Write("-->| ");	write_nuh(line);								writer.Write(" ");		writer.Write(HttpUtility.HtmlEncode(line.Message));								writer.Write("<br>\n"); break;
								case FastLogReader.LineType.Part:		writer.Write("|<-- ");	write_nuh(line);								writer.Write(" ");		writer.Write(HttpUtility.HtmlEncode(line.Message));								writer.Write("<br>\n"); break;
								case FastLogReader.LineType.Quit:		writer.Write("|<-- ");	write_nuh(line);								writer.Write(" ");		writer.Write(HttpUtility.HtmlEncode(line.Message));								writer.Write("<br>\n"); break;
								case FastLogReader.LineType.Kick:		writer.Write("!<-- ");	writer.Write(HttpUtility.HtmlEncode(line.Nick));writer.Write(" ");		writer.Write(HttpUtility.HtmlEncode(line.Message));								writer.Write("<br>\n"); break;
								case FastLogReader.LineType.Meta:		writer.Write("+--+ ");	write_nuh(line);								writer.Write(" ");		writer.Write(HttpUtility.HtmlEncode(line.Message));								writer.Write("<br>\n"); break;
								default:								writer.Write("??? ");																			writer.Write(HttpUtility.HtmlEncode(line.Message));								writer.Write("<br>\n"); break;
								}
							};

							int moreContext = -1;
							Queue<FastLogReader.Line> PreContext = new Queue<FastLogReader.Line>();

							if ( linesOfContext==0 ) writer.WriteLine("	<hr>");
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
									if ( linesOfContext!=0 && PreContext.Count>=linesOfContext && moreContext==-1 ) {
										writer.WriteLine("<hr>");
									}

									while ( PreContext.Count>0 ) {
										write(PreContext.Dequeue());
									}
									if ( highlight_matches ) writer.Write("<div class=\"matched\">");
									write(line);
									if ( highlight_matches ) writer.Write("</div>");
									moreContext = linesOfContext;
								} else if ( moreContext>0 ) { // not a match, but it's post-context
									write(line);
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
							writer.WriteLine("	<hr>");
							writer.WriteLine( "Search matched {1} lines, displayed {2}, searched {3}, and took {0} seconds", (stop2-start2).TotalSeconds.ToString("N2"), linesMatched, linesWritten, linesSearched );
						}
					} else {
						Debug.Assert(thisStop == text.Length);
					}
				}
			}
		}
	}
}
