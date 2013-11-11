using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Globalization;

namespace LoggingMonkey {
    public static class DnsCache {
		static readonly Dictionary<string,string[]> HostToIpv4 = new Dictionary<string,string[]>();
		static readonly AutoResetEvent ARE = new AutoResetEvent(false);

		enum ErrorType
		{
			PermanentKnown,
			TemporaryKnown,
			TemporaryUnknown,
		}

		static ErrorType HandleDnsLookupError( string context, SocketException se ) {
			switch( se.SocketErrorCode ) {
			case SocketError.HostNotFound:
			case SocketError.NoData:
				return ErrorType.PermanentKnown;
			default:
				Debug.WriteLine( string.Format( "WARNING: Unexpected SocketException with SocketErrorCode=={0} in {1}", Enum.GetName(typeof(SocketError),se.SocketErrorCode), context ) );
				return ErrorType.TemporaryUnknown;
			}
		}

		static ErrorType HandleDnsLookupError( string context, Exception e ) {
			Debug.WriteLine( string.Format( "WARNING: Exception with Message=={0} in {1}", Enum.GetName(typeof(SocketError),e.Message), context ) );
			return ErrorType.TemporaryUnknown;
		}

		static bool HandleDnsRobustly( string context, Action action, Action permanentErrorHandler ) {
			try {
				action();
				return true;
			} catch( SocketException se ) {
				if( HandleDnsLookupError( context, se ) == ErrorType.PermanentKnown )
					permanentErrorHandler();
				return false;
#if !DEBUG
			} catch( Exception e ) {
				if( HandleDnsLookupError( context, e ) == ErrorType.PermanentKnown )
					permanentErrorHandler();
				return false;
#endif
			}
		}

		static bool HandleDnsRobustly( string context, Action action ) {
			return HandleDnsRobustly( context, action, ()=>{} );
		}

		public static void Prefetch( string dns ) {
			lock( HostToIpv4 )
				if( !HostToIpv4.ContainsKey(dns) )
					HandleDnsRobustly( "Dns.BeginGetHostEntry", ()=>Dns.BeginGetHostEntry(dns,OnResolve,dns), () => HostToIpv4[dns] = Empty );
		}

		static readonly string[] Empty = new string[0];

		static bool KnownFalseHost( string dns ) {
			return string.IsNullOrEmpty(dns)
				|| dns.EndsWith(".users.afternet.org",false,CultureInfo.InvariantCulture)
				;
		}

		public static string[] ResolveDontWait( string dns ) {
			if( KnownFalseHost(dns) )
				return Empty;

			lock( HostToIpv4 )
				if( HostToIpv4.ContainsKey(dns) )
					return HostToIpv4[dns];

			Prefetch(dns);
			ARE.WaitOne(200);

			// minor chance of being immediately available, retry
			lock( HostToIpv4 )
				if( HostToIpv4.ContainsKey(dns) )
					return HostToIpv4[dns];

			return Empty;
		}

		static void OnResolve( IAsyncResult result ) {
			string dns = (string)result.AsyncState;
			IPHostEntry iphe = null;
			lock( HostToIpv4 ) {
				HandleDnsRobustly( "Dns.EndGetHostEntry", () => iphe = Dns.EndGetHostEntry(result), () => HostToIpv4[dns] = Empty );
				if( iphe != null )
					HostToIpv4[dns]
						= iphe.AddressList
						.Where(addr=>addr.AddressFamily==AddressFamily.InterNetwork)
						.Select(addr=>addr.ToString())
						.ToArray()
						;
				ARE.Set();
			}
		}
	}
}
