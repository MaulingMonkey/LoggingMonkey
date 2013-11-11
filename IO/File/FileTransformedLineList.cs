using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LoggingMonkey
{
	class FileTransformedLineList<T> : IEnumerable<T>
	{
		public readonly FileLineList	FileLineList;
		readonly Func<string,T>			Transformation;

		public string Path { get { return FileLineList.Path; } }

		public FileTransformedLineList( string path, Func<string,T> transformation )
		{
			FileLineList = new FileLineList( path );
			Transformation = transformation;
		}

		public void AppendLine( string line )
		{
			FileLineList.AppendLine( line );
		}

		public void RemoveLines( string line )
		{
			FileLineList.RemoveLines( line );
		}

		public IEnumerator<T> GetEnumerator()
		{
			// TODO: Caching
			return FileLineList.Select( Transformation ).GetEnumerator( );
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
