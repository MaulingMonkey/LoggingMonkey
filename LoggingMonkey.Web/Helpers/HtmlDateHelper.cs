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

            switch (options.DateFormatType)
            {
                case DateFormatTypes.MeridiemPst:
                    format = "M/d/yy hh:mm:ss tt";
                    break;

                case DateFormatTypes.TwentyFourHourUtc:
                    format = "M/d/yy HH:mm:ss";
                    break;
            }

            return date.Value.ToString(format, Program.Culture);
        }
    }
}
