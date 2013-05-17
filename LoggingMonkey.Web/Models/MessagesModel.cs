using System.Collections.Generic;
using System;
using System.Linq;

namespace LoggingMonkey.Web.Models
{
    public class Message
    {
        public Message()
        {
            Lines = new List<string>();
        }

        public bool UsesTor { get; set; }
        public DateTime Timestamp { get; set; }
        public string Nick;
        public FastLogReader.LineType Type;
        public List<string> Lines;
    }

    public class MessagesModel
    {
        public MessagesModel()
        {
            Messages = new List<Message>();
        }

        public string ChannelName { get; set; }

        public decimal TimeElapsed { get; set; }

        public List<Message> Messages { get; set; }

        public bool IsEmpty
        {
            get
            {
                return Messages.Count == 0;
            }
        }

        public IEnumerable<string> ActiveNicks
        {
            get { return Messages.Select(x => x.Nick).Distinct(); }
        }
    }
}