using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LoggingMonkey {
	static class DnsCache {
		static readonly Dictionary<string,string[]> HostToIpv4 = new Dictionary<string,string[]>();

		public static void Prefetch( string dns ) {
			lock( HostToIpv4 ) if( !HostToIpv4.ContainsKey(dns) ) try {
				Dns.BeginGetHostEntry(dns,OnResolve,dns);
			} catch( SocketException se ) {
				if( se.SocketErrorCode == SocketError.HostNotFound )
					lock( HostToIpv4 )
						HostToIpv4[dns] = Empty;
				else
					Debug.WriteLine("WARNING: SocketException on Dns.BeginGetHostEntry");
#if !DEBUG
			} catch( Exception ) {
#endif
			}
		}

		static readonly string[] Empty = new string[0];

		public static string[] ResolveDontWait( string dns ) {
			lock( HostToIpv4 )
				if( HostToIpv4.ContainsKey(dns) )
					return HostToIpv4[dns];

			Prefetch(dns);
			Thread.Sleep(200);

			// minor chance of being immediately available, retry
			lock( HostToIpv4 )
				if( HostToIpv4.ContainsKey(dns) )
					return HostToIpv4[dns];

			return Empty;
		}

		static void OnResolve( IAsyncResult result ) {
			string dns = (string)result.AsyncState;
			IPHostEntry iphe = null;
			lock( HostToIpv4 ) try {
				iphe = Dns.EndGetHostEntry(result);
			} catch( SocketException se ) {
				if( se.SocketErrorCode == SocketError.HostNotFound )
					lock( HostToIpv4 )
						HostToIpv4[dns] = Empty;
				else
					Debug.WriteLine("WARNING: SocketException on Dns.EndGetHostEntry");
				return;
#if !DEBUG
			} catch( Exception ) {
				return;
#endif
			}

			if( iphe != null )
				lock( HostToIpv4 )
					HostToIpv4[dns]
						= iphe.AddressList
						.Where(addr=>addr.AddressFamily==AddressFamily.InterNetwork)
						.Select(addr=>addr.ToString())
						.ToArray()
						;
		}
	}
}
