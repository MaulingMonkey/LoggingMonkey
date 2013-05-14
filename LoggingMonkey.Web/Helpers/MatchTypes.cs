using System.ComponentModel;

namespace LoggingMonkey.Web.Helpers
{
    public enum MatchTypes
    {
        [Description("Plain Text")]
        PlainText,

        [Description("Wildcard")]
        Wildcard,

        [Description("Regex")]
        Regex
    }
}