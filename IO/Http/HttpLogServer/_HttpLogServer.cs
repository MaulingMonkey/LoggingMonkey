using System;
using System.Linq;
using System.Net;

namespace LoggingMonkey {
	partial class HttpLogServer {
		readonly HttpListener Listener;
		readonly HttpRoutesList Handlers;

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
		
		HttpRoutesList CreateDefaultHandlers( )
		{
			return new HttpRoutesList( )
			{
				{ "/"				, AccessControlStatus.Blacklisted,	a => HandleLogsRequest			( a.HttpListenerContext, a.AccessControlStatus, a.Logs ) },
				{ "/auth"			, AccessControlStatus.Blacklisted,	a => { HandleAuthRequest		( a.HttpListenerContext, ref a.AccessControlStatus ); HandleLogsRequest( a.HttpListenerContext, a.AccessControlStatus, a.Logs ); } },
				{ "/backup.zip"		, AccessControlStatus.Admin,		a => HandleBackupRequest		( a.HttpListenerContext, a.AccessControlStatus ) },
				{ "/api/1/logs"		, AccessControlStatus.Admin,		a => HandleJsonLogsRequest		( a.HttpListenerContext, a.AccessControlStatus, a.Logs ) },
				//{ "/v2"			  AccessControlStatus.Admin,		, CreateHandleTemplatecFile("index")	},
				{ "/404"			, AccessControlStatus.Blacklisted,	CreateHandleTemplatecFile("_404")		},
				{ "/robots.txt"		, AccessControlStatus.Blacklisted,	CreateHandleStaticFile("robots")		},
				{ "/04b_03__.ttf"	, AccessControlStatus.Blacklisted,	CreateHandleStaticFile("_04B_03__")		},
				{ "/favicon.png"	, AccessControlStatus.Blacklisted,	CreateHandleStaticFile("favicon")		},
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

		void OnGetContext( IAsyncResult result )
		{
			if ( !Listener.IsListening ) return;

			Listener.BeginGetContext( OnGetContext, null );
			var context = Listener.EndGetContext(result);

			var acs = GetAuth(context);

			AllLogs logs;
			lock( Listener ) logs = _Logs;

			var args = new HttpRequest( )
			{
				HttpListenerContext	= context,
				AccessControlStatus	= acs,
				Logs				= logs,
			};

			try {
				Handlers.Dispatch( context.Request.Url.AbsolutePath.ToLowerInvariant(), args );
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
