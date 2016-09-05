using System;
using System.Collections;
using System.Collections.Generic;

namespace LoggingMonkey {
	class HttpRoutesList : IEnumerable {
		readonly Dictionary<string,Action<HttpRequest>> Routes = new Dictionary<string, Action<HttpRequest>>();

		public bool ContainsRoute(string path) {
			return Routes.ContainsKey(path);
		}

		public void Dispatch(string path, HttpRequest request) {
			if (Routes.ContainsKey(path)) {
				Routes[ path ]( request );
			} else {
				request.HttpListenerContext.Response.StatusCode = 404; // Missing
				Routes["/404"]( request );
			}
		}

		public void Add(string url, AccessControlStatus minimumAccess, Action<HttpRequest> handler) {
			Routes.Add(url, req => {
				if (req.AccessControlStatus <= minimumAccess)	handler(req);
				else											req.HttpListenerContext.Response.StatusCode = 403; // Forbidden
			});
		}

		public IEnumerator GetEnumerator() { return Routes.GetEnumerator(); }
	}
}
