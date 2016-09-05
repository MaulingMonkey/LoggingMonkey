using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;

namespace LoggingMonkey {
	partial class HttpLogServer {
		static readonly string IndexHtmlTemplate = Assets.ResourceManager.GetText("index");
		private void HandleLogsRequest( HttpListenerContext context, AccessControlStatus acs, AllLogs logs )
		{
			var p = new LogRequestParameters(context.Request);

			using ( var writer = new StreamWriter(context.Response.OutputStream) ) {
				var text = IndexHtmlTemplate
					.Replace("{{Network}}",	p.Network)
					.Replace("{{Channel}}",	p.Channel)
					.Replace("{{FromDate}}",p.From.ToString(Program.Culture))
					.Replace("{{ToDate}}",	p.To	.ToString(Program.Culture));

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
						if ( logs==null ) {											writer.WriteLine( "<div class=\"notice\">Logs are currently loading.  Reload this page in a minute.</div>");
						} else lock (logs) if ( !logs.ContainsKey(p.Network) ) {	writer.WriteLine( "<div class=\"notice\">Not serving logs for {0}</div>", p.Network );
						} else if ( !logs[p.Network].HasChannel(p.Channel) ) {		writer.WriteLine( "<div class=\"notice\">Not serving logs for {0}</div>", p.Channel );
						} else {
							var clog = logs[p.Network].Channel(p.Channel);
							if ( clog.RequireAuth )
							{
								if ( !Allow(acs) )
								{
									switch( acs )
									{
									case AccessControlStatus.Admin:			writer.WriteLine( "<div class=\"notice\">Not (yet) authorized to access channel logs for {0}.  You're somehow simultaniously an admin yet not allowed in.</div>",		p.Channel ); break;
									case AccessControlStatus.Whitelisted:	writer.WriteLine( "<div class=\"notice\">Not (yet) authorized to access channel logs for {0}.  You're somehow simultaniously whitelisted yet not allowed in.</div>",	p.Channel ); break;
									case AccessControlStatus.Pending:		writer.WriteLine( "<div class=\"notice\">Not (yet) authorized to access channel logs for {0}.  Authorization cookie set, whitelisting pending.</div>",					p.Channel ); break;
									case AccessControlStatus.Error:			writer.WriteLine( "<div class=\"notice\">Not (yet) authorized to access channel logs for {0}.  PM LoggingMonkey !auth to set an authorization cookie.</div>",			p.Channel ); break;
									case AccessControlStatus.Blacklisted:	writer.WriteLine( "<div class=\"notice\">Not (yet) authorized to access channel logs for {0}.  Authorization cookie set, whitelisting pending...</div>",				p.Channel ); break;
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
						var clog = logs[p.Network].Channel(p.Channel);
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
								switch( p.TimeFmt )
								{
								case "longpst": writer.Write(line.When.ToString("M/d/yy hh:mm:ss tt",Program.Culture)); break;
								case "longutc": writer.Write(TimeZoneInfo.ConvertTimeToUtc( line.When, pst ).ToString("M/d/yy HH:mm:ss",Program.Culture)); break;
								case "pst":		writer.Write(line.When.ToString("M/d h:mm tt",Program.Culture)); break;
								default:		writer.Write(line.When.ToString("M/d h:mm tt",Program.Culture)); break;
								}
								writer.Write("] ");

								switch ( line.Type ) {
								case FastLogReader.LineType.Action:		writer.Write("*");		write_nuh(line);								writer.Write(" ");		writer.Write(Program.HtmlizeUrls(HttpUtility.HtmlEncode(line.Message),p.Cats));	writer.Write("*<br>\n"); break;
								case FastLogReader.LineType.Message:	writer.Write("&lt;");	write_nuh(line);								writer.Write("&gt; ");	writer.Write(Program.HtmlizeUrls(HttpUtility.HtmlEncode(line.Message),p.Cats));	writer.Write("<br>\n"); break;
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

							if ( p.LinesOfContext==0 ) writer.WriteLine("	<hr>");
							bool highlight_matches = (p.NickQuery!=null || p.HostQuery!=null || p.MessQuery!=null) && p.LinesOfContext>0;

							foreach ( var line in FastLogReader.ReadAllLines(p.Network,p.Channel,p.From,p.To) ) {
								bool lineMatch
									=  ( p.From <= line.When && line.When <= p.To )
									&& ( p.NickQuery == null || p.NickQuery.IsMatch(line.Nick   ??"") )
									&& ( p.UserQuery == null || p.UserQuery.IsMatch(line.User   ??"") )
									&& ( p.HostQuery == null || p.HostQuery.IsMatch(line.Host   ??"") )
									&& ( p.MessQuery == null || p.MessQuery.IsMatch(line.Message??"") )
									;

								++linesSearched;
								if ( lineMatch ) {
									++linesMatched;
									// write out pre-context and write line
									if ( p.LinesOfContext!=0 && PreContext.Count>=p.LinesOfContext && moreContext==-1 ) {
										writer.WriteLine("<hr>");
									}

									while ( PreContext.Count>0 ) {
										write(PreContext.Dequeue());
									}
									if ( highlight_matches ) writer.Write("<div class=\"matched\">");
									write(line);
									if ( highlight_matches ) writer.Write("</div>");
									moreContext = p.LinesOfContext;
								} else if ( moreContext>0 ) { // not a match, but it's post-context
									write(line);
									--moreContext;
								} else { // not a match, not immediate post-context, start feeding back into pre-context
									if ( p.LinesOfContext!=0 && PreContext.Count>=p.LinesOfContext ) {
										PreContext.Dequeue();
										moreContext = -1;
									}
									if ( p.LinesOfContext!=0 ) PreContext.Enqueue(line);
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
