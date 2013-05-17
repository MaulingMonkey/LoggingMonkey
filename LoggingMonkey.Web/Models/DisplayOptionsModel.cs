using System;
using System.Web;
using System.Web.Helpers;
using LoggingMonkey.Web.Helpers;

namespace LoggingMonkey.Web.Models
{
    public class DisplayOptionsModel
    {
        public DateFormatTypes DateFormatType { get; set; }

        public string ThemeName { get; set; }

        public bool UseTinyFont { get; set; }

        public bool ShowCats { get; set; }

        public static DisplayOptionsModel FromHttpContext(HttpContextBase context)
        {
            return context.Request.Cookies["LoggingMonkeyDisplay"] != null
                       ? FromJson(context.Request.Cookies["LoggingMonkeyDisplay"].Value)
                       : new DisplayOptionsModel();
        }

        public static string ToJson(DisplayOptionsModel model)
        {
            return Json.Encode(model);
        }

        public static DisplayOptionsModel FromJson(string val)
        {
            return String.IsNullOrWhiteSpace(val) ? null : Json.Decode<DisplayOptionsModel>(val);
        }
    }
}
