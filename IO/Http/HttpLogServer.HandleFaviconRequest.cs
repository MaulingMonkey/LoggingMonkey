using System.Net;

namespace LoggingMonkey {
	partial class HttpLogServer {
		private static void HandleFaviconRequest( HttpListenerContext context )
		{
			var favicon = Assets.ResourceManager.GetObject("favicon") as byte[];
			context.Response.OutputStream.Write( favicon, 0, favicon.Length );
		}
	}
}
