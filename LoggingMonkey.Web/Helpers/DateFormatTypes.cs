using System.ComponentModel;

namespace LoggingMonkey.Web.Helpers
{
    public enum DateFormatTypes
    {
        [Description("M/D/YY H:MM:SS [AM,PM] (PST)")]
        MeridiemPst,

        [Description("M/D/YY H:MM:SS (24h) (UTC)")]
        TwentyFourHourUtc
    }
}
