using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Text;

namespace LoggingMonkey {
	static class ExtensionMethods {
		public static string MustReplace( this string value, string what, string with ) {
			Debug.Assert( value.Contains(what) );
			return value.Replace(what,with);
		}
	}

	class ChannelLogs : List<ChannelLogs.Entry>, IDisposable {
		public struct Entry {
			public DateTime When;
			public string PreambleHtml, NicknameHtml, NihHtml; // Nickname!Identity@Hostname
			public string MessageHtml;

			public string CompleteHtml { get {
				var sb = new StringBuilder();
				sb.Append("[");
				sb.Append(When.ToString("g"));
				sb.Append("]");
				sb.Append(PreambleHtml);
				sb.Append("<a title='");
				sb.Append(NihHtml);
				sb.Append("'>");
				sb.Append(NicknameHtml);
				sb.Append("</a>");
				sb.Append(MessageHtml);
				sb.AppendLine("<br>");
				return sb.ToString();
			}}
		}

		string Network, Channel, FileNamePattern;

		DateTime LastLoggedDate;
		StreamWriter StreamWriter;

		public ChannelLogs( string network, string channel, string filenamepattern ) {
			Network = network;
			Channel = channel;
			FileNamePattern = filenamepattern;
		}

		public void Dispose() {
			using ( StreamWriter ) StreamWriter = null;
		}

		void PrepareWrite( DateTime when ) {
			var today = when.Date;

			if ( LastLoggedDate != today ) {
				using ( StreamWriter ) {}

				LastLoggedDate = today;
				var filename = FileNamePattern
					.Replace    ( "{network}", Network )
					.Replace    ( "{channel}", Channel )
					.MustReplace( "{year}"   , today.Year .ToString() )
					.MustReplace( "{month}"  , today.Month.ToString() )
					.MustReplace( "{day}"    , today.Day  .ToString() )
					;

				StreamWriter = new StreamWriter( File.Open( filename, FileMode.Append, FileAccess.Write, FileShare.Read ), Encoding.UTF8 );
			}
		}

		static readonly Regex group = new Regex(@"\{([a-zA-Z0-9]+)\}",RegexOptions.Compiled); // matches "{name}"
		public void Log( Match input, string outputformat ) {
			var when = DateTime.Now;
			PrepareWrite(when);

			Debug.Assert( string.IsNullOrEmpty(input.Groups["channel"].Value) || input.Groups["channel"].Value == Channel );
			Debug.Assert( string.IsNullOrEmpty(input.Groups["network"].Value) || input.Groups["network"].Value == Network );

			var line = outputformat
				.Replace( "{when}"   , when.ToString("T") )
				.Replace( "{channel}", Channel )
				.Replace( "{network}", Network )
				;

			foreach ( Match match in group.Matches(line) ) {
				var name = match.Groups[1].Value;
				Debug.Assert(input.Groups[name].Success);
				line=line.Replace("{"+name+"}",input.Groups[name].Value);
			}

			StreamWriter.WriteLine( line );
			StreamWriter.Flush();

			line = HttpUtility.HtmlEncode(line);
			line = Program.reWhen.Replace( line, m => "" );
			var mWho = Program.reWho.Match( line );
			Debug.Assert( mWho.Success );
			string nick, nih;
			if ( mWho.Success ) {
				nick = mWho.Groups["nick"].Value;
				nih = mWho.Value;
			} else {
				nick = nih = string.Empty;
			}

			var preamble = string.Intern(line.Substring(0,mWho.Index));
			var message  = line.Substring(mWho.Index+mWho.Length);
			lock (this) Add( new Entry()
				{ When = when
				, NicknameHtml = string.Intern(nick)
				, NihHtml      = string.Intern(nih)
				, PreambleHtml = preamble
				, MessageHtml  = (preamble == " *" || preamble == " &lt;") ? Program.HtmlizeUrls(message) : message
				});
		}
	}
}
