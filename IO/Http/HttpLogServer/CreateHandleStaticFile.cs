using System;
using System.Text;

namespace LoggingMonkey {
	partial class HttpLogServer {
		private static Action<HttpRequest> CreateHandleStaticFile(string id)
		{
			var obj = Assets.ResourceManager.GetObject(id);
			var file = obj as byte[];
			if (file == null) {
				var txt = obj as string;
				if (txt != null) file = Encoding.UTF8.GetBytes(txt);
			}
			if (file == null) throw new Exception("Missing static file: "+id);
			return (request) => request.HttpListenerContext.Response.OutputStream.Write(file, 0, file.Length);
		}
	}
}
