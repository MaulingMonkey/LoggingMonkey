using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace LoggingMonkey {
	class IrcMessageReactor : CompoundIrcMessageReactor {
		static readonly Random RNG = new Random();

		static readonly Regex reTag = new Regex(@"<.+?>",RegexOptions.Compiled);
		static readonly Dictionary<string,DateTime> SpamLimiterList = new Dictionary<string,DateTime>();

		public static readonly IrcMessageReactor Default = new IrcMessageReactor()
			{ { @"^\:(?<who>[^ ]+) PRIVMSG (?<channels>[^ ]+) \:?\001ACTION (?<message>.+)\001$" , "[{when}] *{who} {message}*" }
			, { @"^\:(?<who>[^ ]+) PRIVMSG (?<channel>[^ ]+) \:?!last (?<few>\d+)$", (network,m) => {
				var channel = m.Groups["channel"].Value;
				if( network.Channels.Contains(channel) ) {
					var logs = network.Logs.Channel(channel);
					try {
						var who = m.Groups["who"].Value;
						var nick = who.Substring(0,who.IndexOf('!'));
						var host = who.Substring(who.IndexOf('@')+1);
						var few = Math.Min(10,int.Parse(m.Groups["few"].Value));
						if (!SpamLimiterList.ContainsKey(host)) SpamLimiterList.Add(host,DateTime.Now.AddMinutes(-2));
						var now = DateTime.Now;

						if ( (now-SpamLimiterList[host]).TotalMinutes < 1 ) {
							network.Send( "PRIVMSG "+nick+ " :Stop spamming me ;_;" );
						} else {
							//for ( int i = Math.Max(logs.Count-few,0) ; i < logs.Count ; ++i ) network.Send( "PRIVMSG "+nick+ " :"+HttpUtility.HtmlDecode(reTag.Replace(logs[i].CompleteHtml,"")) );
							network.Send( "PRIVMSG "+nick+" :!logs temporarilly disabled" );
						}

						SpamLimiterList[host]=now;
					} catch ( Exception ) {}
					logs.Log(m,"[{when}] <{who}> !last {few}");
					Console.WriteLine("{0} used !last",m.Groups["who"].Value);
				}
			}}
			, { @"^\:(?<who>[^ ]+) PRIVMSG (?<channel>[^ ]+) \:?!auth$", (network,m) => {
				var channel = m.Groups["channel"].Value;
				if( network.Channels.Contains(channel) )
					network.Logs.Channel(channel).Log(m,"[{when}] <{who}> !auth");

				try {
					var who = m.Groups["who"].Value;
					var nick = who.Substring(0,who.IndexOf('!'));
					var host = who.Substring(who.IndexOf('@')+1);
					var token = AccessControl.RequestToken(who);
					network.Send( "NOTICE "+nick+ " :"+Program.PrimaryPrefix+"auth?token="+HttpUtility.UrlEncode(token) );
					Console.WriteLine("{0} used !auth, responded with token {1}",who,token);
				} catch ( Exception ) {}
			}}
			, { @"^\:(?<who>[^ ]+) PRIVMSG (?<channel>[^ ]+) \:?!whitelist (?<user>[^ ]+)$"      , (network, m) => AccessControl.Whitelist ( m.Groups["who"].Value, m.Groups["user"].Value ) }
			, { @"^\:(?<who>[^ ]+) PRIVMSG (?<channel>[^ ]+) \:?!blacklist (?<user>[^ ]+)$"      , (network, m) => AccessControl.Blacklist ( m.Groups["who"].Value, m.Groups["user"].Value ) }
			, { @"^\:(?<who>[^ ]+) PRIVMSG (?<channel>[^ ]+) \:?!twit (?<user>[^ ]+)$"           , (network, m) => AccessControl.Twitlist  ( m.Groups["who"].Value, m.Groups["user"].Value ) }
			, { @"^\:(?<who>[^ ]+) PRIVMSG (?<channel>[^ ]+) \:?!untwit (?<user>[^ ]+)$"         , (network, m) => AccessControl.Untwitlist( m.Groups["who"].Value, m.Groups["user"].Value ) }
			, { @"^\:(?<who>[^ ]+) PRIVMSG (?<channel>[^ ]+) \:?!twitlist (?<user>[^ ]+)$"       , (network, m) => AccessControl.Twitlist  ( m.Groups["who"].Value, m.Groups["user"].Value ) }
			, { @"^\:(?<who>[^ ]+) PRIVMSG (?<channel>[^ ]+) \:?!untwitlist (?<user>[^ ]+)$"     , (network, m) => AccessControl.Untwitlist( m.Groups["who"].Value, m.Groups["user"].Value ) }
			, { @"^\:(?<who>[^ ]+) PRIVMSG (?<channels>[^ ]+) \:?(?<message>.+)$"                , "[{when}] <{who}> {message}" }
			, { @"^\:(?<who>[^ ]+) JOIN (?<channels>[^ ]+)$"                                     , "[{when}] -->| {who} has joined {channel}" }
			, { @"^\:(?<who>[^ ]+) PART (?<channels>[^ ]+) ?\:?(?<message>.*)$"                  , "[{when}] |<-- {who} has left {channel} ({message})" }
			, { @"^\:(?<who>[^ ]+) KICK (?<channel>[^ ]+) (?<target>[^ ]+) ?\:?(?<message>.*)$"  , "[{when}] !<-- {target} was kicked from {channel} by {who} ({message})"  }
			//, { @"^\:(?<who>[^ ]+) QUIT \:?(?<message>.*)$"                                      , "[{when}] |<-- {who} has quit {network} ({message})" }
			, { @"^\:(?<who>[^ ]+) MODE (?<channel>[^ ]+) (?<modes>.+)$"                         , "[{when}] +--+ {who} has set mode(s) {modes} in {channel}" }
			, { @"^\:(?<who>[^ ]+) TOPIC (?<channel>[^ ]+) ?\:?(?<topic>.*)$"                    , "[{when}] +--+ {who} has set {channel}'s topic to {topic}" }
			, { @"^\:(?<who>[^ ]+) NICK [:]?(?<newnick>[^ ]+)$"                                  , "[{when}] +--+ {who} has changed their nick to {newnick}" }

			, { @"PING(?<data> [^ ]*)?$"                              , (network,match) => network.Send( "PONG" + match.Groups["data"].Value ) }
			, { @"^\:(?<who>[^ ]+) (?<code>\d\d\d) \:?(?<message>.*)$", (network,match) => {
				switch ( match.Groups["code"].Value ) {
				case "001": foreach ( var channel in network.Channels ) network.Send("JOIN "+channel); break;
				case "433": network.Send( "NICK LoggingMonkey"+RNG.Next(9999).ToString() ); break;
				}
			}}
			};
	}
}
