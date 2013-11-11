using System;
using System.Linq;

namespace LoggingMonkey
{
	static class Platform
	{
		/// <summary>
		/// Detect UNIXy platform
		/// http://www.mono-project.com/FAQ:_Technical (see "How to detect the execution platform ?")
		/// </summary>
		public static readonly bool IsOnUnix = new[]{PlatformID.Unix,PlatformID.MacOSX,(PlatformID)128}.Any(p=>Environment.OSVersion.Platform == p);
	}
}
