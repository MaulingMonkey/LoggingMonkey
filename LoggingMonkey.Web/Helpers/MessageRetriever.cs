﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LoggingMonkey.Web.Models;

namespace LoggingMonkey.Web.Helpers
{
    public static class MessageRetriever
    {
        private static readonly CachedHashedWebCsvFile Tor = new CachedHashedWebCsvFile (Path.Combine(Path.GetTempPath(), "tor.csv"), @"http://torstatus.blutmagie.de/ip_list_all.php/Tor_ip_list_ALL.csv");

        public static MessagesModel Get(SearchModel search)
        {
            var output = new MessagesModel();

            var channelName = search.ChannelId == 0 ? ChannelHelper.GetById(1) : ChannelHelper.GetById(search.ChannelId);
            var networkName = "irc.afternet.org";

            var prevFromDate = search.FromDate;
            var prevToDate = search.ToDate;

            search.FromDate = search.FromDate ?? DateTime.Now.AddMinutes(-15);
            search.ToDate   = search.ToDate   ?? DateTime.Now.AddMinutes(15);

            var lines = FastLogReader.ReadAllLines(networkName, channelName, search.FromDate.Value, search.ToDate.Value);

            Process(output, Filter(lines, search));

            search.FromDate = prevFromDate;
            search.ToDate = prevToDate;

            return output;
        }
        private static IEnumerable<FastLogReader.Line> Filter(IEnumerable<FastLogReader.Line> lines, SearchModel search)
        {
            var regexOptions = RegexOptions.Compiled | (search.IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);

            Func<string, Regex> buildStringMatcherFor = input =>
            {
                if (String.IsNullOrEmpty(input)) return null;
                switch (search.MatchType)
                {
                    case MatchTypes.PlainText: return new Regex(Regex.Escape(input), regexOptions);
                    case MatchTypes.Wildcard: return new Regex("^" + Regex.Escape(input).Replace(@"\*", "(.*)").Replace(@"\?", ".") + "$", regexOptions);
                    case MatchTypes.Regex: return new Regex(input, regexOptions);
                    default: goto case MatchTypes.PlainText;
                }
            };

            var nickMatcher = buildStringMatcherFor(search.Nickname);
            var userMatcher = buildStringMatcherFor(search.Username);
            var hostMatcher = buildStringMatcherFor(search.Hostname);
            var msgMatcher  = buildStringMatcherFor(search.Message);

            foreach (var line in lines)
            {
                if (line.When < search.FromDate || line.When > search.ToDate) continue;

                if (nickMatcher != null && !nickMatcher.IsMatch(line.Nick   ?? String.Empty))   continue;
                if (userMatcher != null && !userMatcher.IsMatch(line.User   ?? String.Empty))   continue;
                if (hostMatcher != null && !hostMatcher.IsMatch(line.Host   ?? String.Empty))   continue;
                if (msgMatcher  != null && !msgMatcher.IsMatch(line.Message ?? String.Empty))   continue;

                yield return line;
            }
        }

        private static void Process(MessagesModel model, IEnumerable<FastLogReader.Line> lines)
        {
            //
            // This process could be refactored into some black LINQ magic, but
            // for now, just keep track of previous Nick and Type.
            //

            Message msg = null;
            FastLogReader.LineType prevType = FastLogReader.LineType.Meta;
            string prevNick = null;
            DateTime? prevWhen = null;

            foreach (var line in lines)
            {
                var isTor = Tor.Lines.Contains(line.Host) || DnsCache.ResolveDontWait(line.Host).Any(ipv4 => Tor.Lines.Contains(ipv4));

                if (msg == null)
                {
                    msg = new Message { Timestamp = line.When, Nick = line.Nick, Type = line.Type };

                    if (isTor)
                    {
                        msg.UsesTor = true;
                    }
                }

                if (prevWhen == null)
                {
                    prevWhen = line.When;
                }

                if (prevNick != null && line.Nick == prevNick && line.Type == prevType && line.When.Subtract(prevWhen.Value).Minutes <= 1)
                {
                    msg.Lines.Add(line.Message);
                    continue;
                }

                msg = new Message();

                prevNick = line.Nick;
                prevType = line.Type;
                prevWhen = line.When;

                if (isTor)
                {
                    msg.UsesTor = true;
                }

                msg.Type = line.Type;
                msg.Nick = line.Nick;
                msg.Timestamp = line.When;
                msg.Lines.Add(line.Message);

                model.Messages.Add(msg);
            }
        }
    }
}