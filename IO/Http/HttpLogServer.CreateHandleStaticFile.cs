using System;
using System.Net;

namespace LoggingMonkey {
	partial class HttpLogServer {
		private static Action<HttpRequest> CreateHandleStaticFile(string id)
		{
			var file = Assets.ResourceManager.GetObject(id) as byte[];
			return (request) => request.HttpListenerContext.Response.OutputStream.Write(file, 0, file.Length);
		}
	}
}
