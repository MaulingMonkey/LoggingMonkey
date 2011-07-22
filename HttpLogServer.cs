using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace LoggingMonkey {
	class HttpLogServer {
		readonly HttpListener Listener;

		public HttpLogServer() {
#if DEBUG
			Listener = new HttpListener()
				{ Prefixes = { "http://logs2.pandamojo.com" + (Program.IsOnUnix?":8080":"") + "/" }
				};
#else
			Listener = new HttpListener()
				{ Prefixes = { "http://logs.pandamojo.com" + (Program.IsOnUnix?":8080":"") + "/" }
				};
#endif
			Listener.Start();
			Listener.BeginGetContext(OnGetContext,null);
		}

		AllLogs _Logs;
		public void SetLogs( AllLogs logs ) {
			lock (Listener) {
				Debug.Assert(_Logs==null);
				_Logs=logs;
			}
		}

		void OnGetContext( IAsyncResult result ) {
			if ( !Listener.IsListening ) return;

			Listener.BeginGetContext( OnGetContext, null );
			var context = Listener.EndGetContext(result);

			AllLogs logs;
			lock (Listener) logs = _Logs;

			try {
				// Handle special cases:
				switch ( context.Request.Url.AbsolutePath.ToLowerInvariant() ) {
				case "/":
					break; // we'll handle this normally
				case "/robots.txt":
					context.Response.ContentEncoding = Encoding.UTF8;
					context.Response.ContentType = "text/plain";
					using ( var writer = new StreamWriter(context.Response.OutputStream, Encoding.UTF8) ) {
						writer.Write
							( "User-agent: *\n"
							+ "Disallow: /\n"
							);
					}
					return; // EARLY BAIL
				case "/04b_03__.ttf":
					var font = Assets.ResourceManager.GetObject("_04B_03__") as byte[];
					context.Response.OutputStream.Write(font,0,font.Length);
					return; // EARLY BAIL
				case "/backup.zip":
					// TODO: Handle multiple backup.zip requests
					Stream zip = null;
					try {
						zip = File.Open( Program.BackupPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None );
					} catch ( IOException ) {
						context.Response.ContentEncoding = Encoding.UTF8;
						context.Response.ContentType = "text/plain";
						context.Response.StatusCode = 503;
						using ( var writer = new StreamWriter(context.Response.OutputStream, Encoding.UTF8) ) {
							writer.Write("Couldn't open backup zip (already backing up?)\n");
						}
						return; // EARLY BAIL
					}

					using ( zip ) {
						using ( var package = ZipPackage.Open(zip,FileMode.Create) ) {
							foreach ( var logfile in Directory.GetFiles(Program.LogsDirectory,"*.log",SearchOption.TopDirectoryOnly) ) {
								var relfile = Uri.EscapeDataString( Path.GetFileName(logfile) );
								var uri = PackUriHelper.CreatePartUri( new Uri(relfile,UriKind.Relative) );
								var part = package.CreatePart( uri, System.Net.Mime.MediaTypeNames.Text.Plain, CompressionOption.Maximum );
								using ( var fstream = File.Open(logfile,FileMode.Open,FileAccess.Read,FileShare.ReadWrite) ) using ( var partstream = part.GetStream() ) fstream.CopyTo(partstream);
								package.Flush();
							}
							package.Close();
						}
						zip.Flush();
						zip.Position = 0;

						context.Response.ContentType = "application/zip";
						context.Response.ContentLength64 = zip.Length;
						zip.CopyTo(context.Response.OutputStream);
					}
					return; // EARLY BAIL
				default:
					context.Response.StatusCode = 404;
					context.Response.ContentEncoding = Encoding.UTF8;
					context.Response.ContentType = "text/html";
					using ( var writer = new StreamWriter(context.Response.OutputStream, Encoding.UTF8) ) {
						writer.Write
							(  "<html><head>\n"
							+  "	<title>No such page</title>\n"
							+  "</head><body>\n"
							+  "	No such page "+context.Request.Url.AbsoluteUri+"\n"
							+  "</body></html>\n"
							);
					}
					return; // EARLY BAIL
				}

				// handle normally:
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
				string hostquerys = vars["hostquery"] ?? null;
				string querys     = vars["query"]     ?? null;
				string querytype  = vars["querytype"] ?? "plaintext";

				Func<string,bool> bools = s => new[]{"true","1"}.Contains((vars[s]??"").ToLowerInvariant());

				bool casesensitive = bools("casesensitive");
				bool cats          = bools("cats");
				bool tiny          = bools("tiny");

				var options
					= RegexOptions.Compiled
					| (casesensitive?RegexOptions.None:RegexOptions.IgnoreCase)
					;

				Regex nickquery = string.IsNullOrEmpty( nickquerys ) ? null : querytype=="regex" ? new Regex(nickquerys,options) : new Regex(Regex.Escape(nickquerys),options);
				Regex hostquery = string.IsNullOrEmpty( hostquerys ) ? null : querytype=="regex" ? new Regex(hostquerys,options) : new Regex(Regex.Escape(hostquerys),options);
				Regex query     = string.IsNullOrEmpty( querys     ) ? null : querytype=="regex" ? new Regex(querys    ,options) : new Regex(Regex.Escape(querys    ),options);

				using ( var writer = new StreamWriter(context.Response.OutputStream) ) {
					writer.WriteLine("<html><head>");
					writer.WriteLine("\t<title>{0} -- {1} ({2} - {3})</title>", network, channel, from, to );
					writer.WriteLine("\t<meta http-equiv=\"Content-Type\" content=\"text/html;charset=UTF-8\">");
					writer.WriteLine("\t<script type=\"text/javascript\" src=\"http://cdn.jquerytools.org/1.2.5/jquery.tools.min.js\"></script>");
					writer.WriteLine("\t<style type=\"text/css\">");
					if ( tiny ) {
						writer.WriteLine("\t	@font-face { font-family: \"04b03\"; src: url(\"/04B_03__.TTF\") format(\"truetype\"); }");
						writer.WriteLine("\t	* { font-family: \"04b03\"; font-size: 8px; }");
					}
					writer.WriteLine("\t	table, tr, td { cell-spacing: 0; padding: 0; margin: 0; border-collapse: collapse; vertical-align: top; }");
					writer.WriteLine("\t	a { color: blue; }");
					writer.WriteLine("\t	.link { color: red; }");
					writer.WriteLine("\t	.tooltip { display: none; background: black; color: white; padding: 5px; }");
					writer.WriteLine("\t	.matched { background-color: #B0FFB0; }");
					writer.WriteLine("\t</style>");
					writer.WriteLine("</head><body>");
					writer.WriteLine("	<div style=\"7pt; float: right; text-align: right\">");
					writer.WriteLine("		Contact: MaulingMonkey in #gamedev [<a href=\"irc://irc.afternet.org/gamedev\">irc://</a>][<a href=\"http://www.gamedev.net/community/chat/\">java</a>]<br>");
					writer.WriteLine("	</div>");
					writer.WriteLine("	<form method=\"get\" action=\".\">");
					writer.WriteLine("		<table><tr>");
					writer.WriteLine("			<td><table>");
					writer.WriteLine("				<tr><td><label>Network:</label></td><td><input name=\"server\"  value=\"{0}\"></td></tr>", network);

					//writer.WriteLine("				<tr><td><label>Channel:</label></td><td><input name=\"channel\" value=\"{0}\"></td></tr>", channel );
					if (logs==null || !logs.ContainsKey(network) ) {
						writer.WriteLine("				<tr><td><label>Channel:</label></td><td><input name=\"channel\" value=\"{0}\"></td></tr>", channel );
					} else {
						writer.WriteLine("				<tr><td><label>Channel:</label></td><td><select name=\"channel\">{0}</select></td></tr>", string.Join("",logs[network].Channels.Select(ch=>((ch==channel)?"<option selected=\"true\">":"<option>")+ch+"</option>").ToArray()) );
					}

					writer.WriteLine("				<tr><td></td><td>Search Parameters</td></tr>");
					writer.WriteLine("				<tr><td><label>Nickname:</label></td><td><input name=\"nickquery\"   value=\"{0}\"></td></tr>", nickquerys ?? "" );
					writer.WriteLine("				<tr><td><label>Hostname:</label></td><td><input name=\"hostquery\"   value=\"{0}\"></td></tr>", hostquerys ?? "" );
					writer.WriteLine("				<tr><td><label>Message:</label></td><td><input  name=\"query\"       value=\"{0}\"></td></tr>", querys     ?? "" );
					writer.WriteLine("				<tr><td></td><td>");
					writer.WriteLine("					    <input name=\"casesensitive\" value=\"true\"      type=\"checkbox\" {0}> <label>Case Sensitive</label>", casesensitive          ? "checked" : "" );
					writer.WriteLine("					<br><input name=\"querytype\"     value=\"plaintext\" type=\"radio\"    {0}> <label>Plain Text</label>"    , querytype=="plaintext" ? "checked" : "" );
					writer.WriteLine("					<br><input name=\"querytype\"     value=\"regex\"     type=\"radio\"    {0}> <label>Regex Match</label>"   , querytype=="regex"     ? "checked" : "" );
					writer.WriteLine("				</td></tr>");
					writer.WriteLine("			</table></td><td><table>");
					writer.WriteLine("				<tr><td><label>From:   </label></td><td><input name=\"from\"    value=\"{0}\"></td></tr>", from.ToString(Program.Culture) );
					writer.WriteLine("				<tr><td><label>To:     </label></td><td><input name=\"to\"      value=\"{0}\"></td></tr>", to  .ToString(Program.Culture) );
					writer.WriteLine("				<tr><td><label>Context:</label></td><td><input name=\"context\" value=\"{0}\"></td></tr>", linesOfContext );
					writer.WriteLine("				<tr><td>                       </td><td><input type=\"submit\"  value=\"Search\"></td></tr>");
					writer.WriteLine("			</table></td>");
					writer.WriteLine("		</tr></table>");
					writer.WriteLine("	</form>");

					ChannelLogs clog = null;
					if ( logs==null ) {
						writer.WriteLine( "	Logs are currently loading.  Reload this page in a minute.");
					} else lock (logs) if ( !logs.ContainsKey(network) ) {
						writer.WriteLine( "	Not serving logs for {0}", network );
					} else if ( !logs[network].HasChannel(channel) ) {
						writer.WriteLine( "	Not serving logs for {0}", channel );
					} else {
						clog = logs[network].Channel(channel);
					}

					if ( clog!=null ) {
						var start2 = DateTime.Now;
						int linesMatched = 0;
						int linesWritten = 0;
						int linesSearched= 0;

						Action<FastLogReader.Line> write_nuh = (line) => {
							writer.Write("<a title='");
							writer.Write(HttpUtility.HtmlEncode(string.Format("{0}!{1}@{2}",line.Nick,line.User,line.Host)));
							writer.Write("'>");
							writer.Write(HttpUtility.HtmlEncode(line.Nick));
							writer.Write("</a>");
						};

						Action<FastLogReader.Line> write = (line) => {
							++linesWritten;

							writer.Write("[");
							writer.Write(line.When.ToString("g"));
							writer.Write("] ");

							switch ( line.Type ) {
							case FastLogReader.LineType.Action:
								writer.Write("*");
								write_nuh(line);
								writer.Write(" ");
								writer.Write(Program.HtmlizeUrls(HttpUtility.HtmlEncode(line.Message),cats));
								writer.Write("*<br>\n");
								break;
							case FastLogReader.LineType.Message:
								writer.Write("&lt;");
								write_nuh(line);
								writer.Write("&gt; ");
								writer.Write(Program.HtmlizeUrls(HttpUtility.HtmlEncode(line.Message),cats));
								writer.Write("<br>\n");
								break;
							case FastLogReader.LineType.Join:
								writer.Write("-->| ");
								write_nuh(line);
								writer.Write(" ");
								writer.Write(HttpUtility.HtmlEncode(line.Message));
								writer.Write("<br>\n");
								break;
							case FastLogReader.LineType.Part:
							case FastLogReader.LineType.Quit:
								writer.Write("|<-- ");
								write_nuh(line);
								writer.Write(" ");
								writer.Write(HttpUtility.HtmlEncode(line.Message));
								writer.Write("<br>\n");
								break;
							case FastLogReader.LineType.Kick:
								writer.Write("!<-- ");
								writer.Write(HttpUtility.HtmlEncode(line.Nick));
								writer.Write(" ");
								writer.Write(HttpUtility.HtmlEncode(line.Message));
								writer.Write("<br>\n");
								break;
							case FastLogReader.LineType.Meta:
								writer.Write("+--+ ");
								write_nuh(line);
								writer.Write(" ");
								writer.Write(HttpUtility.HtmlEncode(line.Message));
								writer.Write("<br>\n");
								break;
							default:
								writer.Write("??? ");
								writer.Write(HttpUtility.HtmlEncode(line.Message));
								writer.Write("<br>\n");
								break;
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

					writer.WriteLine("	<script type='text/javascript'> $(document).ready(function() { $('a[title]').tooltip(); });</script>");
					writer.WriteLine("</body></html>");
				}
			} catch ( Exception e ) {
				if ( Program.IsOnUnix ) {
					File.AppendAllText( Program.ExceptionsPath, e.ToString() );
				}
			} finally {
				context.Response.Close();
			}
		}
	}
}
