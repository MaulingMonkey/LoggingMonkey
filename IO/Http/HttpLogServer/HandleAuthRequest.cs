using System;
using System.Net;

namespace LoggingMonkey {
	partial class HttpLogServer {
		private static void HandleAuthRequest( HttpListenerContext context, ref AccessControlStatus acs )
		{
			var token = context.Request.QueryString["token"];
			if (string.IsNullOrEmpty(token)) return;

			var expiration = DateTime.UtcNow.AddYears(10).ToString("ddd, dd-MMM-yyyy H:mm:ss"); // http://stackoverflow.com/questions/4811009/c-sharp-httplistener-cookies-expiring-after-session-even-though-expiration-time
			context.Response.Headers.Add("Set-Cookie", string.Format("{0}={1};Path=/;Expires={2} GMT",Program.AuthCookieName,token,expiration));
			acs = AccessControl.GetStatus(token);
		}
	}
}
