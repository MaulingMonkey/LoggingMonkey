using System;
using System.IO;
using System.Text.RegularExpressions;

namespace LoggingMonkey {
	partial class HttpLogServer {
		static readonly Regex reTemplateParam = new Regex(@"\{\{(?<id>[a-zA-Z0-9_.-]+)\}\}", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		private static Action<HttpRequest> CreateHandleTemplatecFile(string assetId)
		{
			var text = Assets.ResourceManager.GetText(assetId);

			return (request) => {
				var w = new StreamWriter(request.HttpListenerContext.Response.OutputStream);
				var rewritten = reTemplateParam.Replace(text, ev => {
					var id = ev.Groups["id"].Value;

					switch (id) {
					case "Request.Url.AbsoluteUri":	return request.HttpListenerContext.Request.Url.AbsoluteUri;
					case "Request.Url.Host":		return request.HttpListenerContext.Request.Url.Host;
					default:						return "{{"+id+"}}";
					}
				});
				w.Write(rewritten);
				w.Flush();
			};
		}
	}
}
