using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LoggingMonkey
{
	public class FileAccessList
	{
		static readonly RegexOptions RegexOptions = RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase;

		public string Description { get; private set; }
		readonly FileTransformedLineList< Regex > FRLL;

		static Regex WildcardsToRegex( string line )
		{
			return new Regex( string.Format( "^{0}$", Regex.Escape( line ).Replace( @"\*", @"(.*)" ) ), RegexOptions );
		}

		public FileAccessList( string description, string path )
		{
			Description = description;
			FRLL = new FileTransformedLineList<Regex>( path, WildcardsToRegex );
		}

		public IEnumerable<string> Dump()
		{
			return FRLL.FileLineList.ToArray();
		}

		public void AppendLine( string line )
		{
			FRLL.AppendLine( line );
		}

		public void RemoveLines( string line )
		{
			FRLL.RemoveLines( line );
		}

		public bool ContainsUser( string line )
		{
			return FRLL.Any( re => re.IsMatch( line ) );
		}

		public bool ContainsLine( string line )
		{
			return FRLL.FileLineList.Contains( line );
		}
	}
}
