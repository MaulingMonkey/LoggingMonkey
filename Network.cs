using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LoggingMonkey {
	class Network {
		string Hostname;
		int    Port;

		TcpClient Client;
		NetworkStream NetworkStream;
		StreamReader StreamReader;
		StreamWriter StreamWriter;
		Timer Pinger;

		public NetworkLogs Logs { get; private set; }
		readonly HashSet<string> _Channels = new HashSet<string>();
		public IEnumerable<string> Channels { get { return _Channels; }}

		public Network( string hostname, string[] channels, AllLogs logs ) {
			Hostname = hostname;
			Port     = 6667;
			Reconnect();

			Logs = logs[hostname];
			_Channels = new HashSet<string>(channels);
			Pinger = new Timer(o=>TrySend("PING LoggingMonkey"),null,0,60000);
		}

		public void TrySend( string message ) {
			try {
				Send(message);
			} catch ( Exception ) {
			}
		}

		public void Send( string message ) {
			lock ( StreamWriter ) {
				var buffer = Encoding.UTF8.GetBytes(message+"\r\n");
				int i=0;
				while ( i<buffer.Length ) i += Client.Client.Send(buffer,i,buffer.Length,SocketFlags.None);
			}
		}

		Exception LastException = null;
		public void Work() {
			for (;;)
			try
			{
				if ( Client == null || !Client.Connected ) {
					Reconnect();
					continue;
				}
				var line = StreamReader.ReadLine();
				if ( line == null ) {
					// Client.Connected lied, stupid bitch!
					Reconnect();
					continue;
				}

				IrcMessageReactor.Default.TryReact( this, line );
			}
			catch ( Exception e )
			{
				var se = e as SocketException ?? e.InnerException as SocketException;
				bool same = (LastException!=null) && e.Message == LastException.Message;
				same = false;

				Console.Write(same?"\r":"\n");

				if ( se!=null ) switch ( se.SocketErrorCode ) {
				case SocketError.HostNotFound: Console.Write( "[{0}] No such host is known (intertubes are probably down)", DateTime.Now ); break;
				default:                       Console.Write( "[{0}] Miscellanious socket error: {1}", DateTime.Now, se.SocketErrorCode ); break;
				} else {
					Console.Write( "[{0}] Miscellanious exception: {1}", DateTime.Now, e.Message );
				}
				LastException = e;
			}
		}

		void Reconnect() {
			using ( StreamWriter ) StreamWriter = null;
			using ( StreamReader ) StreamReader = null;
			using ( NetworkStream ) NetworkStream = null;
			using ( Client ) Client = null;

			Client = new TcpClient(Hostname,Port);
			NetworkStream = new NetworkStream(Client.Client);
			StreamReader = new StreamReader(NetworkStream, Encoding.UTF8);
			StreamWriter = new StreamWriter(NetworkStream, Encoding.UTF8);

			if ( Password != null ) Send( "PASS "+Password );
			Send( "USER monkey * * *" );
			Send( "NICK LoggingMonkey" );
			if ( Password != null ) Send( "PASS "+Password );
		}

		static string Password = null;
		static Network() {
			try { Password = File.ReadAllText(@"I:\home\scripts\bmpass.txt"); } catch ( Exception ) {}
		}
	}
}
