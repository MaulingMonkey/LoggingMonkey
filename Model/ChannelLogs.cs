﻿using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace LoggingMonkey {
	public static class ExtensionMethods {
		public static string MustReplace( this string value, string what, string with ) {
			Debug.Assert( value.Contains(what) );
			return value.Replace(what,with);
		}
	}

	public class ChannelLogs : IDisposable {
		public bool RequireAuth = false;

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

				if ( File.Exists(filename.Replace("#","%23")) ) filename = filename.Replace("#","%23");
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
				.Replace( "{when}"   , when.ToString("T",Program.Culture) )
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
		}
	}
}
