using System.Collections.Generic;

namespace LoggingMonkey.Web.Helpers
{
    public static class ChannelHelper
    {
        private static readonly Dictionary<int, string> ChannelIdNamePairs;

        static ChannelHelper()
        {
            ChannelIdNamePairs = new Dictionary<int, string>
            {
                {1, "#gamedev"},
                {2, "#graphicschat"},
                {3, "#anime"},
                {4, "#starcraft"}
            };
        }

        public static string GetById(int id)
        {
            return ChannelIdNamePairs[id];
        }

        public static Dictionary<int, string> GetAll()
        {
            return ChannelIdNamePairs;
        }
    }
}