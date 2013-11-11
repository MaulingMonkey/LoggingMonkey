using System;
using System.Net;
using System.Web;

namespace LoggingMonkey {
	partial class HttpLogServer {
		private static void HandleAuthRequest( HttpListenerContext context, ref AccessControlStatus acs )
		{
			var m = reAuthQuery.Match( context.Request.Url.Query ?? "" );
			if( m.Success && m.Groups["token"].Success )
			{
				var expiration = DateTime.UtcNow.AddYears(10).ToString("ddd, dd-MMM-yyyy H:mm:ss"); // http://stackoverflow.com/questions/4811009/c-sharp-httplistener-cookies-expiring-after-session-even-though-expiration-time
				var token = HttpUtility.UrlDecode(m.Groups["token"].Value);
				context.Response.Headers.Add("Set-Cookie", string.Format("{0}={1};Path=/;Expires={2} GMT",Program.AuthCookieName,token,expiration));
				acs = AccessControl.GetStatus(token);
			}
		}
	}
}
