using System.IO;
using System.Net;
using System.Text;

namespace LoggingMonkey {
	partial class HttpLogServer {
		private static void HandleInvalidPageRequest( HttpListenerContext context )
		{
			context.Response.StatusCode = 404;
			context.Response.ContentEncoding = Encoding.UTF8;
			context.Response.ContentType = "text/html";
			using ( var writer = new StreamWriter(context.Response.OutputStream, Encoding.UTF8) ) {
				writer.Write
					(  "<html><head>\n"
					+  "	<title>No such page</title>\n"
					+  "</head><body>\n"
					+  "	No such page "+context.Request.Url.AbsoluteUri+"<br>\n"
					+  "	Try <a href=\"/\">"+context.Request.Url.Host+"</a> instead you silly git<br>\n"
					+  "</body></html>\n"
					);
			}
		}
	}
}
