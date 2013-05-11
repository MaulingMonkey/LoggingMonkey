using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LoggingMonkey
{
	class FileLineList : IEnumerable<string>
	{
		static readonly List<string> Empty = new List<string>();

		private object Lock = new object();
		public string Path { get; private set; }

		public FileLineList( string path )
		{
			Path = path;
		}

		public void AppendLine( string line )
		{
			Debug.Assert(!line.Contains("\r"));
			Debug.Assert(!line.Contains("\n"));
			lock( Lock )
			using( var writer = new StreamWriter( Path, true, Encoding.UTF8 ) )
			{
				writer.WriteLine(line);
			}
		}

        public void RemoveLines(string line)
        {
            lock (Lock)
            try
            {
                var lines = File.ReadAllLines(Path).Where(x => x != line).ToList();

                using (var writer = new StreamWriter(Path, false, Encoding.UTF8))
                {
                    lines.ForEach(writer.WriteLine);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

		public IEnumerator<string> GetEnumerator()
		{
			// TODO: Caching
			lock( Lock )
			try
			{
				return File.ReadAllLines(Path).Cast<string>().GetEnumerator();
			}
			catch( Exception )
			{
				return Empty.GetEnumerator();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
