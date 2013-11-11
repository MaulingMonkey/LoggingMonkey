using System.Net;

namespace LoggingMonkey {
	partial class HttpLogServer {
		private static void HandleFontRequest( HttpListenerContext context )
		{
			var font = Assets.ResourceManager.GetObject( "_04B_03__" ) as byte[];
			context.Response.OutputStream.Write( font, 0, font.Length );
		}
	}
}
