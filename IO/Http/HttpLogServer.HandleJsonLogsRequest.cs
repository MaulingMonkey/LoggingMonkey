using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace LoggingMonkey {
	partial class HttpLogServer {
		private void HandleJsonLogsRequest( HttpListenerContext context, AccessControlStatus acs, AllLogs logs )
		{
			var p = new LogRequestParameters(context.Request);

			using ( var writer = new StreamWriter(context.Response.OutputStream) ) {
				var notices = new List<string>();

				writer.WriteLine(@"{");

				ChannelLogs clog = null;
				if ( logs==null ) {											notices.Add(string.Format("Logs are currently loading.  Reload this page in a minute."));
				} else lock (logs) if ( !logs.ContainsKey(p.Network) ) {	notices.Add(string.Format("Not serving logs for {0}", p.Network));
				} else if ( !logs[p.Network].HasChannel(p.Channel) ) {		notices.Add(string.Format("Not serving logs for {0}", p.Channel));
				} else {
					writer.WriteLine(@"	""channels"": [{0}],", logs == null ? "" : string.Join(",",logs[p.Network].Channels.Select(Json.ToString)));
					clog = logs[p.Network].Channel(p.Channel);
					if ( clog.RequireAuth && !Allow(acs) )
					{
						switch( acs )
						{
						case AccessControlStatus.Admin:			notices.Add(string.Format("Not (yet) authorized to access channel logs for {0}.  You're somehow simultaniously an admin yet not allowed in.",		p.Channel )); break;
						case AccessControlStatus.Whitelisted:	notices.Add(string.Format("Not (yet) authorized to access channel logs for {0}.  You're somehow simultaniously whitelisted yet not allowed in.",	p.Channel )); break;
						case AccessControlStatus.Pending:		notices.Add(string.Format("Not (yet) authorized to access channel logs for {0}.  Authorization cookie set, whitelisting pending.",					p.Channel )); break;
						case AccessControlStatus.Error:			notices.Add(string.Format("Not (yet) authorized to access channel logs for {0}.  PM LoggingMonkey !auth to set an authorization cookie.",			p.Channel )); break;
						case AccessControlStatus.Blacklisted:	notices.Add(string.Format("Not (yet) authorized to access channel logs for {0}.  Authorization cookie set, whitelisting pending...",				p.Channel )); break;
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
						switch( p.TimeFmt )
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
							var nextLineTags = new List<string>();
							if ( p.LinesOfContext!=0 && PreContext.Count>=p.LinesOfContext && moreContext==-1 ) {
								nextLineTags.Add("break");
							}

							while ( PreContext.Count>0 ) {
								write(PreContext.Dequeue(), nextLineTags);
								nextLineTags.Clear();
							}
							nextLineTags.Add("matched");
							write(line, nextLineTags);
							nextLineTags.Clear();
							moreContext = p.LinesOfContext;
						} else if ( moreContext>0 ) { // not a match, but it's post-context
							write(line, new List<string>());
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

					if (linesWritten > 0) writer.WriteLine();
					writer.WriteLine("	],"); // end of "chat" array
					writer.WriteLine("	\"stats\": {{ \"matched\": {1}, \"displayed\": {2}, \"searched\": {3}, \"time\": {0} }}", (stop2-start2).TotalSeconds.ToString("N2"), linesMatched, linesWritten, linesSearched );
				}

				writer.WriteLine("}");
			}
		}
	}
}
