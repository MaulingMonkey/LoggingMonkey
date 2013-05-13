using System;
using System.Collections.Generic;

namespace LoggingMonkey {
	public class NetworkLogs : IDisposable {
		string Network, FileNamePattern;

		readonly Dictionary<string,ChannelLogs> _Channels = new Dictionary<string,ChannelLogs>();

		public IEnumerable<String> Channels { get { return _Channels.Keys; } }

		public NetworkLogs( string network, string filenamepattern ) {
			Network = network;
			FileNamePattern = filenamepattern;
		}

		public void Dispose() {
			foreach ( var channellog in _Channels.Values )
			using ( channellog )
			{
			}
			_Channels.Clear();
		}

		public bool HasChannel( string channel ) {
			channel = channel.ToLowerInvariant();

			return _Channels.ContainsKey(channel);
		}

		public ChannelLogs Channel( string channel ) {
			channel = channel.ToLowerInvariant();

			if (!_Channels.ContainsKey(channel)) _Channels.Add
				( channel
				, new ChannelLogs
					( Network
					, channel
					, FileNamePattern
						.Replace    ("{network}",Network)
						.MustReplace("{channel}",channel)
					)
				);
			return _Channels[channel];
		}
	}
}
