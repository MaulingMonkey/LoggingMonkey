using System;
using System.Web.Mvc;
using LoggingMonkey.Web.Models;

namespace LoggingMonkey.Web.Helpers
{
    public static class HtmlDateHelper
    {
        public static string GetDateFromDisplayOptions(this HtmlHelper helper, DateTime? date)
        {
            if (!date.HasValue)
            {
                return String.Empty;
            }

            var options = DisplayOptionsModel.FromHttpContext(helper.ViewContext.HttpContext);

            var format = "M/d h:mm tt";
            var tz = TimeZoneInfo.Utc;

            switch (options.DateFormatType)
            {
                case DateFormatTypes.MinutesOnlyPst:
                    format = "M/d h:mm tt";
                    tz = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
                    break;

                case DateFormatTypes.MeridiemPst:
                    format = "M/d/yy hh:mm:ss tt";
                    tz = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
                    break;

                case DateFormatTypes.TwentyFourHourUtc:
                    format = "M/d/yy HH:mm:ss";
                    tz = TimeZoneInfo.Utc;
                    break;
            }

            return TimeZoneInfo.ConvertTimeFromUtc(date.Value.ToUniversalTime(), tz).ToString(format, Program.Culture);
        }
    }
}
