using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LoggingMonkey {
	struct FastLineReader {
		string Line;
		int LineIndex;

		public static implicit operator FastLineReader( string line ) { return new FastLineReader(){Line=line,LineIndex=0,Character=line.Length>0?line[0]:'\0'}; }

		public char Character;

		void Advance() {
			++LineIndex;
			Character = ( LineIndex<Line.Length ) ? Line[LineIndex] : '\0';
		}

		void Error() {
			LineIndex=Line.Length;
			Character = '\0';
		}

		public bool Eat( char ch ) {
			bool advance = Character==ch;
			if ( advance ) Advance();
			return advance;
		}

		public bool Eat( string s ) {
			int index = LineIndex;
			var chr   = Character;

			foreach ( var ch in s ) if (!Eat(ch)) {
				LineIndex = index;
				Character = chr;
				return false;
			}

			return true;
		}

		public char EatAny() {
			var ch=Character;
			Advance();
			return ch;
		}

		public string EatUntil( char ch ) {
			var start = LineIndex;
			var end   = Line.IndexOf(ch,start);
			if ( end==-1 ) {
				Error();
				return null;
			} else {
				LineIndex = end;
				Character = Line[end];
				return Line.Substring(start,end-start);
			}
		}

		public string EatUntilDiscard( char ch ) {
			var eu = EatUntil(ch);
			if ( eu!=null ) Eat(ch);
			return eu;
		}

		public string EatRemainderUntil( char ch ) {
			Debug.Assert( Line[Line.Length-1] == ch );
			return Line.Substring(LineIndex,Line.Length-LineIndex-1);
		}

		public string EatRemainder() {
			return Line.Substring(LineIndex);
		}
	}

	public static class FastLogReader {
		static readonly Regex reLogFilename = new Regex(@".*\\(?<network>[^-\\]+)-(?<channel>#[^-\\]+)-(?<year>\d+)-(?<month>\d+)-(?<day>\d+)\.log",RegexOptions.Compiled);

		public enum LineType {
			Message, Action,
			Join, Part, Quit, Kick,
			Meta,
		}

		public struct Line {
			public LineType Type;
			public DateTime When;
			public string Nick, User, Host, Message;

			public override string ToString() {
				return string.Format
					( "{0} Line @ {1} {2}!{3}@{4}  {5}"
					, Type
					, When
					, Nick
					, User
					, Host
					, Message
					);
			}
		}

		static int FastParseDigits( string s ) {
			if (!s.All(ch=>'0'<=ch&&ch<='9')) throw new ArgumentException("Not only digits");
			int i=0;
			foreach ( var ch in s ) i=10*i+(ch-'0');
			return i;
		}

		class FileEntry {
			public DateTime Date { get { return new DateTime(Year,Month,Day); }}
			public int Year,Month,Day;
			public string Name, Network, Channel;
		}

		static FileEntry[] GetFiles( string network, string channel ) {
			return Directory
				.GetFiles(@"I:\home\logs\", "*.log", SearchOption.TopDirectoryOnly )
				.Select( file => {
					var m = reLogFilename.Match(file);
					return new FileEntry()
						{ Name  = file
						, Year  = int.Parse(m.Groups["year" ].Value)
						, Month = int.Parse(m.Groups["month"].Value)
						, Day   = int.Parse(m.Groups["day"  ].Value)
						, Network = m.Groups["network"].Value
						, Channel = m.Groups["channel"].Value
						};
				})
				.Where( file => file.Network==network && file.Channel==channel )
				.OrderBy( file => file.Date )
				.ToArray()
				;
		}

		public static IEnumerable<Line> ReadAllLines( string network, string channel, DateTime start, DateTime end ) {
			FileEntry[] files = GetFiles(network,channel);

			foreach ( var file in files ) {
				if ( file.Date < start.Date ) continue;
				if ( file.Date > end  .Date ) continue;

				using ( var reader = new StreamReader(File.Open(file.Name,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)) )
				for ( string rawline ; (rawline=reader.ReadLine()) != null ; )
				{
					FastLineReader line = rawline;

					int H=0,M=0,S=0;

					if (!line.Eat('[')) continue;
					while ( '0'<=line.Character && line.Character<='9' ) H=H*10+(line.EatAny()-'0');
					if (!line.Eat(':')) continue;
					while ( '0'<=line.Character && line.Character<='9' ) M=M*10+(line.EatAny()-'0');
					if (line.Eat(':')) while ( '0'<=line.Character && line.Character<='9' ) S=S*10+(line.EatAny()-'0');
					line.Eat(' ');

					bool am = line.Character=='a' || line.Character=='A';
					bool pm = line.Character=='p' || line.Character=='P';
					if (H==12) H-=12;
					if (pm) H+=12;

					if (!am&&!pm) continue;
					line.EatAny();
					if (!(line.Eat('m') || line.Eat('M'))) continue;
					if (!(line.Eat(']') && line.Eat(' '))) continue;

					var toyield = new Line()
						{ When = new DateTime( file.Year, file.Month, file.Day, H, M, S )
						};

					Action<char> EatNuhUntil = chr => {
						toyield.Nick    = line.EatUntilDiscard('!');
						toyield.User    = line.EatUntilDiscard('@');
						toyield.Host    = line.EatUntilDiscard(chr);
					};

					switch ( line.Character ) {
					case '<': // message
						line.Eat('<');
						toyield.Type    = LineType.Message;
						EatNuhUntil('>');
						if (!line.Eat(' ')) continue;
						toyield.Message = line.EatRemainder();
						break;
					case '*': // action
						line.Eat('*');
						toyield.Type    = LineType.Action;
						EatNuhUntil(' ');
						toyield.Message = line.EatRemainderUntil('*');
						break;
					case '|': // part or quit
						if (!line.Eat("|<-- ")) continue;
						toyield.Type    = LineType.Part;
						EatNuhUntil(' ');
						toyield.Message = line.EatRemainder();
						if ( toyield.Message.StartsWith("has quit") ) toyield.Type = LineType.Quit;
						break;
					case '-': // join
						if (!line.Eat("-->| ")) continue;
						toyield.Type    = LineType.Join;
						EatNuhUntil(' ');
						toyield.Message = line.EatRemainder();
						break;
					case '!': // kick
						if (!line.Eat("!<-- ")) continue;
						toyield.Type    = LineType.Kick;
						toyield.Nick    = line.EatUntil(' ');
						toyield.Message = line.EatRemainder();
						break;
					case '+': // meta
						if (!line.Eat("+--+ ")) continue;
						toyield.Type    = LineType.Meta;
						EatNuhUntil(' '); // FIXME: Might not actually be a NUH (e.g. "+--+ *.afternet.org has set ...)
						toyield.Message = line.EatRemainder();
						break;
					default:
						continue;
					}

					yield return toyield;
				}
			}
		}
	}
}
