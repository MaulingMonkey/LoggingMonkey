﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Diagnostics;

namespace LoggingMonkey {
	class Program {
		/// <summary>
		/// Detect UNIXy platform
		/// http://www.mono-project.com/FAQ:_Technical (see "How to detect the execution platform ?")
		/// </summary>
		public static readonly bool IsOnUnix = new[]{PlatformID.Unix,PlatformID.MacOSX,(PlatformID)128}.Any(p=>Environment.OSVersion.Platform == p);

		public static readonly string PasswordPath  = IsOnUnix ? @"/home/loggingmonkey/password.txt"    : @"I:\home\scripts\bmpass.txt";
		public static readonly string BackupPath    = IsOnUnix ? @"/home/loggingmonkey/logs-backup.zip" : @"I:\home\logs-backup.zip";
		public static readonly string LogsDirectory = IsOnUnix ? @"/home/loggingmonkey/logs/"           : @"I:\home\logs\";

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

		public static string HtmlizeUrls( string text, bool cats ) {
			return reUrlPatterns.Replace( text, m => {
				var url=GuessAndPrependProtocol(m.Value);

				if ( cats && url.StartsWith("http://zao.se/~zao/cats/") ) return "<img src=\""+url+"\" alt=\"cats\">";

				return "<a rel=\"nofollow\" class=\"link\" target=\"_blank\" href=\""+url+"\">"+m.Value+"</a>";
			});
		}

		static void Main() {
			bool cancel = false;
			Console.CancelKeyPress += (sender,args) => {
				cancel=true;
				args.Cancel = true;
			};

			var procstart = DateTime.Now;
			Console.WriteLine( "=== Process start at {0} ===", procstart );

			var logpattern = LogsDirectory+"{network}-{channel}-{year}-{month}-{day}.log";
#if DEBUG
			var channels = new[] { "#sparta" };
#else
			var channels = new[] { "#gamedev", "#graphicschat", "#graphicsdev", "#anime", "#starcraft" };
#endif
			var logs = new AllLogs() { { "irc.afternet.org", new NetworkLogs("irc.afternet.org",logpattern) } };
			var afternet = logs["irc.afternet.org"];
			foreach ( var ch in channels ) afternet.Channel(ch);
			afternet.Channel("#gamedev");

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
				.GetFiles(LogsDirectory, "*.log", SearchOption.TopDirectoryOnly )
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



			var bglogs = new AllLogs() { { "irc.afternet.org", new NetworkLogs("irc.afternet.org",logpattern) } };
			var bgafternet = bglogs["irc.afternet.org"];
			foreach ( var ch in channels ) bgafternet.Channel(ch);



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
					Console.WriteLine("\tquit                        Quits");
					break;
				case "quit":
					return;
				default:
					Console.WriteLine( "No such command: {0}", split[0] );
					break;
				}
			}
		}
	}
}
