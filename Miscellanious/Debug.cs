using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace LoggingMonkey {
	static class Debug {
		static object Mutex = new object( );
		static StreamWriter DebugLog;

		static Debug( )
		{
			DebugLog = new StreamWriter( Paths.DebugTxt, true, Encoding.UTF8 );
		}

		public static void Assert
			( bool					condition
			, [CallerMemberName]	string	callerMemberName	= ""
			, [CallerFilePath]		string	callerFilePath		= ""
			, [CallerLineNumber]	int		callerLineNumber	= 0
			)
		{
			if( condition ) return; // assert passed
			WriteLine( "{0}:{1} {2}: Assert failed", callerFilePath, callerLineNumber, callerMemberName );
		}

		public static void WriteLine( string format, params object[] args ) { WriteLine( string.Format( format, args ) ); }
		public static void WriteLine( string what ) { Write( what + "\r\n" ); }
		public static void Write( string format, params object[] args ) { Write( string.Format( format, args ) ); }
		public static void Write( string what )
		{
			lock( Mutex )
			{
				Console.Write( what );

				if( DebugLog != null ) try
				{
					DebugLog.Write( what );
					DebugLog.Flush( );
				}
				catch( Exception e )
				{
					// Disk full? Bad stream? Memory corruption?  We're in a bad state.
					try { DebugLog.Close( ); } catch( Exception ) {}
					DebugLog = null; // Don't continue trying to log, avoids infinite cycle
					LogException( e );
				}
			}
		}

		public static void LogReleaseExceptions( Action a )
		{
#if !DEBUG
			lock( Mutex ) try {
#endif
				a( );
#if !DEBUG
			} catch( Exception e ) {
				LogException( e );
				throw;
			}
#endif
		}

		public static void LogException( Exception e )
		{
			lock( Mutex )
			{
				WriteLine( "Exception:" );

				while( e != null )
				{
					WriteLine( "  Type:    {0}", e.GetType( ) );
					WriteLine( "  Message: {0}", e.Message );
					WriteLine( "  Stack:" );
					foreach( var line in e.StackTrace )
						WriteLine( "    {0}", line );

					e = e.InnerException;
					if( e != null )
						WriteLine( "Inner Exception:" );
				}
			}
		}
	}
}
