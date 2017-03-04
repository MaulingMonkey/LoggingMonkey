using SharpRaven;
using SharpRaven.Data;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace LoggingMonkey {
	static class Debug {
		static object Mutex = new object( );
		static StreamWriter DebugLog;
		static readonly RavenClient RavenClient;

		static Debug( )
		{
			DebugLog = new StreamWriter( Paths.DebugTxt, true, Encoding.UTF8 );
			RavenClient = new RavenClient("https://770b8784200742ea90ef39ddc9b44bdb:579d099c68554f35ba6cf62a9012cb17@sentry.io/110850")
			{
#if DEBUG
				Environment = "DEBUG",
#else
				Environment = "RELEASE",
#endif
			};
		}

		static string StripAbsPrefix( string path ) {
			var prefix = "LoggingMonkey\\";
			var index = path.IndexOf(prefix);
			return (index==-1) ? path : path.Substring( index + prefix.Length );
		}

		public static void Assert
			( bool					condition
			, [CallerMemberName]	string	callerMemberName	= ""
			, [CallerFilePath]		string	callerFilePath		= ""
			, [CallerLineNumber]	int		callerLineNumber	= 0
			)
		{
			if( condition ) return; // assert passed
			RavenClient.Capture(new SentryEvent(new SentryMessage("[0] {1}:{2} {3}: Assert failed"
				, DateTime.Now
				, StripAbsPrefix( callerFilePath )
				, callerLineNumber
				, callerMemberName
				)));
			DoWriteLine
				( "[0] {1}:{2} {3}: Assert failed"
				, DateTime.Now
				, StripAbsPrefix( callerFilePath )
				, callerLineNumber
				, callerMemberName
				);
		}

		public static void WriteLine( string format, params object[] args )	{ WriteLine( string.Format( format, args ) ); }
		public static void WriteLine( string what )							{ Write( what + "\r\n" ); }
		public static void Write( string format, params object[] args )		{ Write( string.Format( format, args ) ); }
		public static void Write( string what )
		{
			if (!string.IsNullOrWhiteSpace(what)) {
				var bc = new Breadcrumb("debug") {
					Level	= BreadcrumbLevel.Info,
					Message	= what.Trim("\r\n\t ".ToCharArray()),
				};
				RavenClient.AddTrail(bc);
			}
			DoWrite( what );
		}

		static void DoWriteLine( string format, params object[] args )	{ WriteLine( string.Format( format, args ) ); }
		static void DoWriteLine( string what )							{ Write( what + "\r\n" ); }
		static void DoWrite( string format, params object[] args )		{ Write( string.Format( format, args ) ); }
		static void DoWrite( string what )
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

		public static void LogExceptions( Action a )
		{
			try {
				a( );
			} catch( Exception e ) when (LogException(e)) { throw; }
		}

		static bool LogException( Exception e )
		{
			lock( Mutex )
			{
				RavenClient.Capture(new SentryEvent(e));

				DoWriteLine( "Exception:" );

				while( e != null )
				{
					DoWriteLine( "  Type:    {0}", e.GetType( ) );
					DoWriteLine( "  Message: {0}", e.Message );
					DoWriteLine( "  Stack:" );
					foreach( var line in e.StackTrace )
						DoWriteLine( "    {0}", line );

					e = e.InnerException;
					if( e != null )
						DoWriteLine( "Inner Exception:" );
				}
			}
			return false;
		}
	}
}
