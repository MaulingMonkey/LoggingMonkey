using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace LoggingMonkey {
	partial class HttpLogServer {
		public class HandlerArgs {
			public HttpListenerContext	HttpListenerContext;
			public AccessControlStatus	AccessControlStatus;
			public AllLogs				Logs;
		}

		readonly HttpListener Listener;
		readonly Dictionary< string, Action< HandlerArgs > > Handlers;

		public HttpLogServer() {
			Handlers = CreateDefaultHandlers( );
			Listener = new HttpListener()
				{ Prefixes = { Program.PrimaryPrefix }
				};
			Listener.Start();
			Listener.BeginGetContext(OnGetContext,null);
		}

		AllLogs _Logs;
		public void SetLogs( AllLogs logs ) {
			lock (Listener) {
				Debug.Assert(_Logs==null);
				_Logs=logs;
			}
		}
		
		Dictionary< string, Action< HandlerArgs > > CreateDefaultHandlers( )
		{
			return new Dictionary<string,Action<HandlerArgs>>( )
			{
				{ "/"              , a => HandleLogsRequest			( a.HttpListenerContext, a.AccessControlStatus, a.Logs ) },
				{ "/auth"          , a => { HandleAuthRequest		( a.HttpListenerContext, ref a.AccessControlStatus ); HandleLogsRequest( a.HttpListenerContext, a.AccessControlStatus, a.Logs ); } },
				{ "/robots.txt"    , a => HandleRobotsRequest		( a.HttpListenerContext ) },
				{ "/04b_03__.ttf"  , a => HandleFontRequest			( a.HttpListenerContext ) },
				{ "/favicon.png"   , a => HandleFaviconRequest		( a.HttpListenerContext ) },
				{ "/backup.zip"    , a => HandleBackupRequest		( a.HttpListenerContext, a.AccessControlStatus ) },
				{ "/404"           , a => HandleInvalidPageRequest	( a.HttpListenerContext ) },
			};
		}

		AccessControlStatus GetAuth( HttpListenerContext context )
		{
			// Determine auth
			Cookie cookie = context.Request.Cookies.OfType< Cookie >( ).FirstOrDefault( c => c.Name == Program.AuthCookieName );
			AccessControlStatus acs = cookie == null ? AccessControlStatus.Error : AccessControl.GetStatus( cookie.Value );
			return acs;
		}

		bool Allow( AccessControlStatus acs )
		{
			switch( acs )
			{
			case AccessControlStatus.Admin:
			case AccessControlStatus.Whitelisted:
				return true;
			case AccessControlStatus.Pending:
			case AccessControlStatus.Error:
				return Program.AutoAllow;
			case AccessControlStatus.Blacklisted:
			default:
				return false;
			}
		}

		static readonly Regex reAuthQuery = new Regex( @"^\?token=(?<token>.*)$", RegexOptions.Compiled );

		void OnGetContext( IAsyncResult result )
		{
			if ( !Listener.IsListening ) return;

			Listener.BeginGetContext( OnGetContext, null );
			var context = Listener.EndGetContext(result);

			var acs = GetAuth(context);

			AllLogs logs;
			lock( Listener ) logs = _Logs;
			
			// Handle special cases:
			var path = context.Request.Url.AbsolutePath.ToLowerInvariant();
			if( !Handlers.ContainsKey(path) )
				path = "/404";

			var args = new HandlerArgs( )
			{
				HttpListenerContext	= context,
				AccessControlStatus	= acs,
				Logs				= logs,
			};

			try {
				Handlers[ path ]( args );
#if !DEBUG
			} catch ( Exception e ) {
				if ( Platform.IsOnUnix ) {
					File.AppendAllText( Paths.ExceptionsTxt, e.ToString() );
				} else if( Debugger.IsAttached ) {
					Debugger.Break();
				}
#endif
			} finally {
				context.Response.Close();
			}
		}
	}
}
