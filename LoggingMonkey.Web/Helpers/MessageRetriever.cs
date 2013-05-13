using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LoggingMonkey.Web.Models;

namespace LoggingMonkey.Web.Helpers
{
    public static class MessageRetriever
    {
        public static MessagesModel Get(SearchModel search)
        {
            var output = new MessagesModel();

            var logpattern = Paths.LogsDirectory + "{network}-{channel}-{year}-{month}-{day}.log";
            var logs = new AllLogs() { { "irc.afternet.org", new NetworkLogs("irc.afternet.org", logpattern) } };
            var afternet = logs["irc.afternet.org"];

            var channels = new[] { "#sparta" };
            var whitelistChannels = new[] { "#sparta" };

            foreach (var ch in channels) afternet.Channel(ch);
            foreach (var ch in whitelistChannels) afternet.Channel(ch).RequireAuth = true;
            afternet.Channel("#gamedev");

            var lines = FastLogReader.ReadAllLines("irc.afternet.org", "#gamedev", DateTime.Now.AddMinutes(-15), DateTime.Now.AddMinutes(15));

            //
            // Organize by date, then group by nearest minute, by name and by type.
            //

            Process(output, lines);

            return output;
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

            foreach (var line in lines)
            {
                if (msg == null)
                {
                    msg = new Message { Timestamp = line.When, Nick = line.Nick, Type = line.Type };
                }

                if (prevNick != null && line.Nick == prevNick && line.Type == prevType)
                {
                    msg.Lines.Add(line.Message);
                    continue;
                }

                msg = new Message();

                prevNick = line.Nick;
                prevType = line.Type;

                switch (line.Type)
                {
                    case FastLogReader.LineType.Message:
                        msg.Type = line.Type;
                        msg.Nick = line.Nick;
                        msg.Timestamp = line.When;
                        msg.Lines.Add(line.Message);

                        model.Messages.Add(msg);
                        break;

                    case FastLogReader.LineType.Join:
                        msg.Type = line.Type;
                        msg.Nick = line.Nick;
                        msg.Timestamp = line.When;

                        model.Messages.Add(msg);
                        break;

                    case FastLogReader.LineType.Quit:
                        msg.Type = line.Type;
                        msg.Nick = line.Nick;
                        msg.Timestamp = line.When;

                        model.Messages.Add(msg);
                        break;

                    case FastLogReader.LineType.Part:
                        msg.Type = FastLogReader.LineType.Quit;
                        msg.Nick = line.Nick;
                        msg.Timestamp = line.When;

                        model.Messages.Add(msg);
                        break;
                }
            }
        }
    }
}