﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace LoggingMonkey {
	partial class HttpLogServer {
		readonly HttpListener Listener;
		readonly Dictionary< string, Action< HttpRequest > > Handlers;

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
		
		Dictionary< string, Action< HttpRequest > > CreateDefaultHandlers( )
		{
			return new Dictionary<string,Action<HttpRequest>>( )
			{
				{ "/"              , a => HandleLogsRequest			( a.HttpListenerContext, a.AccessControlStatus, a.Logs ) },
				{ "/auth"          , a => { HandleAuthRequest		( a.HttpListenerContext, ref a.AccessControlStatus ); HandleLogsRequest( a.HttpListenerContext, a.AccessControlStatus, a.Logs ); } },
				{ "/backup.zip"    , a => HandleBackupRequest		( a.HttpListenerContext, a.AccessControlStatus ) },
				{ "/api/1/logs"    , a => HandleJsonLogsRequest		( a.HttpListenerContext, a.AccessControlStatus, a.Logs ) },
				//{ "/v2"            , CreateHandleTemplatecFile("index")		},
				{ "/404"           , CreateHandleTemplatecFile("_404")		},
				{ "/robots.txt"    , CreateHandleStaticFile("robots")		},
				{ "/04b_03__.ttf"  , CreateHandleStaticFile("_04B_03__")	},
				{ "/favicon.png"   , CreateHandleStaticFile("favicon")		},
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

			var args = new HttpRequest( )
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