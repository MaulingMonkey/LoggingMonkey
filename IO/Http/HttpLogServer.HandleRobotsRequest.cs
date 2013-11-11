using System.IO;
using System.Net;
using System.Text;

namespace LoggingMonkey {
	partial class HttpLogServer {
		private static void HandleRobotsRequest( HttpListenerContext context )
		{
			context.Response.ContentEncoding = Encoding.UTF8;
			context.Response.ContentType = "text/plain";
			using ( var writer = new StreamWriter(context.Response.OutputStream, Encoding.UTF8) ) {
				writer.Write
					( "User-agent: *\n"
					+ "Disallow: /\n"
					);
			}
		}
	}
}
