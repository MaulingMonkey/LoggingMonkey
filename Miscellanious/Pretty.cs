namespace LoggingMonkey {
	class Pretty {
		public static string FormatMemory( long amount ) {
			long k=1000, m=k*1000, g=m*1000, t=g*1000;

			if ( amount < 10*k ) return (amount/1).ToString("N0")+" B";
			if ( amount < 10*m ) return (amount/k).ToString("N0")+" KB";
			if ( amount < 10*g ) return (amount/m).ToString("N0")+" MB";
			if ( amount < 10*t ) return (amount/g).ToString("N0")+" GB";
			return (amount/t).ToString("N")+" TB";
		}
	}
}
