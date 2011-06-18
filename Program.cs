using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Diagnostics;

namespace LoggingMonkey {
	class Program {
		public static readonly string fWhen = @"\[(?<when>[^\]]+)\]";
		public static readonly Regex reWhen = new Regex("^"+fWhen, RegexOptions.Compiled);
		public static readonly Regex reLogFilename = new Regex(@".*\\(?<network>[^-\\]+)-(?<channel>#[^-\\]+)-(?<year>\d+)-(?<month>\d+)-(?<day>\d+)\.log",RegexOptions.Compiled);
		public static readonly Regex reWho = new Regex(@"(?<nick>[^*;! ]+)!(?<user>[^@ ]+)@(?<host>[^&> ]+)", RegexOptions.Compiled);
		public static readonly Regex reUrlProtocol = new Regex("^"+fUrlProtocol,RegexOptions.Compiled);
		public static readonly Regex reUrlPatterns = new Regex(@"\b(?:" + fUrlProtocol + "|"  + fUrlTLD + "|"  + fUrlBLD + ")", RegexOptions.Compiled);

		const string fUrlContinue = "(?:[^.,;:!?')\"\\s]|(\\S(?=\\S|$)))";
		const string fUrlProtocol = @"([-.+a-zA-Z0-9]+?:\/\/"+fUrlContinue+"+)";
		const string fUrlTLD      = @"([^\s]+?\.(?:com|net|org|edu|gov|mil|info|biz)"+fUrlContinue+"*)";
		const string fUrlBLD      = @"((?:www|ftp)\."+fUrlContinue+"+)";

		static string GuessAndPrependProtocol( string url ) {
			Match m = reUrlProtocol.Match(url);
			if ( m.Success ) return url;
			else if ( url.StartsWith("www.") ) return "http://"+url;
			else if ( url.StartsWith("ftp.") ) return "ftp://"+url;
			else return "http://"+url;
		}

		public static string HtmlizeUrls( string text ) {
			return reUrlPatterns.Replace( text, m => { var url=GuessAndPrependProtocol(m.Value); return "<a rel=\"nofollow\" class=\"link\" target=\"_blank\" href=\""+url+"\">"+m.Value+"</a>"; } );
		}

		static void Main() {
			bool cancel = false;
			Console.CancelKeyPress += (sender,args) => {
				cancel=true;
				args.Cancel = true;
			};

			var procstart = DateTime.Now;
			Console.WriteLine( "=== Process start at {0} ===", procstart );

			var logpattern = @"I:\home\logs\{network}-{channel}-{year}-{month}-{day}.log";
#if DEBUG
			var channels = new[] { "#sparta" };
#else
			var channels = new[] { "#gamedev", "#graphicschat", "#graphicsdev", "#anime", "#starcraft" };
#endif
			var logs = new AllLogs() { { "irc.afternet.org", new NetworkLogs("irc.afternet.org",logpattern) } };
			var afternet = logs["irc.afternet.org"];
			foreach ( var ch in channels ) afternet.Channel(ch);



			Console.Write( "Beginning log server..." );
			var server = new HttpLogServer();
			Console.WriteLine( "\rLog server started.                             " );



			Console.Write("LoggingMonkey comming online...");
			var bot = new Network( "irc.afternet.org", channels, logs );
			var bott = new Thread(bot.Work);
			bott.Start();
			Console.WriteLine("\rLoggingMonkey online.                            ");



			Console.Write("Getting directory list...");
			var files = Directory
				.GetFiles(@"I:\home\logs\", "*.log", SearchOption.TopDirectoryOnly )
				.OrderBy( file => {
					var m = reLogFilename.Match(file);
					return new DateTime
						( int.Parse(m.Groups["year"].Value)
						, int.Parse(m.Groups["month"].Value)
						, int.Parse(m.Groups["day"].Value)
						);
				})
				.ToArray()
				;



			Console.Write("\rPreparing to read and format logs...");
			var start = DateTime.Now;
			var filelogs = new List<ChannelLogs.Entry>[files.Length];
			Parallel.For( 0, files.Length, i => {
				var file = files[i];

				if ( cancel ) return;

				Console.Write("\rReading & formatting {0} of {1}...", i+1, files.Length );
				var fileformat = reLogFilename.Match(file);

				var lines = new List<string>();
				filelogs[i] = new List<ChannelLogs.Entry>();
				try {
					using ( var reader = new StreamReader(File.Open(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)) ) {
						string s;
						while ( (s=reader.ReadLine()) != null ) lines.Add(s);
					}
				} catch ( Exception ) {
					return; // squelch exceptions
				}

				foreach ( var line_ in lines ) {
					string line = line_;
					var when = reWhen.Match(line);
					if (!when.Success) continue;

					var time = DateTime.Parse(when.Groups["when"].Value);
					var dt = new DateTime
						( int.Parse( fileformat.Groups["year" ].Value )
						, int.Parse( fileformat.Groups["month"].Value )
						, int.Parse( fileformat.Groups["day"  ].Value )
						, time.Hour
						, time.Minute
						, time.Second
						);

					if ( dt > procstart ) continue;

					line = HttpUtility.HtmlEncode(line);
					line = reWhen.Replace( line, m => "" );
					var mWho = Program.reWho.Match( line );
					string nick, nih;
					if ( mWho.Success ) {
						nick = mWho.Groups["nick"].Value;
						nih = mWho.Value;
					} else {
						nick = nih = string.Empty;
					}

					var preamble = string.Intern(line.Substring(0,mWho.Index));
					var message  = line.Substring(mWho.Index+mWho.Length);

					filelogs[i].Add( new ChannelLogs.Entry()
						{ When = dt
						, NicknameHtml = string.Intern(nick)
						, NihHtml      = string.Intern(nih)
						, PreambleHtml = preamble
						, MessageHtml  = (preamble == " *" || preamble == " &lt;") ? Program.HtmlizeUrls(message) : message
						});
				}
			});
			var bglogs = new AllLogs() { { "irc.afternet.org", new NetworkLogs("irc.afternet.org",logpattern) } };
			var bgafternet = bglogs["irc.afternet.org"];
			foreach ( var ch in channels ) bgafternet.Channel(ch);

			for ( int i = 0 ; i < files.Length ; ++i ) {
				Console.Write( "\rJoining logs together -- {0} of {1}...", i+1, files.Length );
				var file = files[i];
				var m = reLogFilename.Match(file);

				var network_name = m.Groups["network"].Value;
				if ( !bglogs.ContainsKey(network_name) ) bglogs.Add
					( network_name
					, new NetworkLogs(network_name,logpattern)
					);
				var network = bglogs[network_name];

				var channel_name = m.Groups["channel"].Value;
				var channel = network.Channel(channel_name);
				channel.AddRange(filelogs[i]);
			}
			var stop = DateTime.Now;
			Console.WriteLine("\rRead formatted and joined {0} logs in {1} seconds.        ", files.Length, (stop-start).TotalSeconds.ToString("N2") );



			Console.Write("Merging with bot's logs...");
			foreach ( var bgnetwork in bglogs ) {
				var network = logs[bgnetwork.Key];
				foreach ( var channelname in network.Channels ) {
					var channel   =   network.Channel(channelname);
					var bgchannel = bgnetwork.Value.Channel(channelname);
					lock ( channel ) channel.InsertRange(0,bgchannel);
				}
			}
			Console.WriteLine("\rMerged with bot's logs.                  ");



			Console.Write("Starting GC...");
			var before = GC.GetTotalMemory(false);
			var after  = GC.GetTotalMemory(true);
			Console.WriteLine("\rFinished GC. Before: {0}  After: {1}  Saved: {2}"
				, Pretty.FormatMemory(before)
				, Pretty.FormatMemory(after)
				, Pretty.FormatMemory(before-after)
				);

			server.SetLogs(logs);
			Console.WriteLine("Logs now being served.");

			for (;;) {
				Console.Write("> ");
				var command = Console.ReadLine();
				cancel = false;
				var split = command.Split(new[]{' '});

				switch ( split[0] ) {
				case "help":
					Console.WriteLine("\t  Command                     Description");
					Console.WriteLine("\thelp                        displays this command list");
					Console.WriteLine("\tsearch <channel> <query>    does a regex search");
					Console.WriteLine("\tquit                        Quits");
					break;
				case "quit":
					return;
				case "search":
					var log = afternet.Channel(split[1]);
					var s2 = command.Split(new[]{' '},3);
					int lines = 0;
					if ( s2.Length < 3 ) {
						Console.WriteLine("Usage: search <channel> <query>");
						break;
					}
					Regex re = new Regex( s2[2], RegexOptions.Compiled );

					cancel = false;
					var start2 = DateTime.Now;
					foreach ( var entry in log ) {
						if ( cancel ) break;
						if ( re.IsMatch(entry.CompleteHtml) ) {
							Console.WriteLine(entry.CompleteHtml);
							++lines;
						}
					}
					var stop2 = DateTime.Now;
					Console.WriteLine( "Search returned {1} lines and took {0} seconds", (stop2-start2).TotalSeconds.ToString("N2"), lines );

					break;
				default:
					Console.WriteLine( "No such command: {0}", split[0] );
					break;
				}
			}
		}
	}
}
