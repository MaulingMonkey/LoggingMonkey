using System;
using System.IO;
using System.Linq;

namespace LoggingMonkey {
	partial class HttpLogServer {
		private Action<HttpRequest> CreateListAccessControl(string list)
		{
			var acl = AccessControl.FileAccessLists[list];
			return req => {
				var o = new StreamWriter(req.HttpListenerContext.Response.OutputStream);
				o.WriteLine("{");
				o.WriteLine("	\"description\": {0},", Json.ToString(acl.Description));
				o.Write("	\"lines\": [");
				o.Write(string.Join(",", acl.Dump().Select(l => "\n\t\t"+Json.ToString(l))));
				o.WriteLine();
				o.WriteLine("	]");
				o.WriteLine("}");
				o.Flush();
			};
		}
	}
}
