using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace LoggingMonkey.Web.Helpers
{
    public static class HtmlListHelpers
    {
        public static List<SelectListItem> ChannelsList(this HtmlHelper helper)
        {
            return ChannelHelper.GetAll().Select(channel => new SelectListItem {Text = channel.Value, Value = channel.Key.ToString(CultureInfo.InvariantCulture)}).ToList();
        }

        public static List<SelectListItem> ThemesList(this HtmlHelper helper)
        {
            var output = new List<SelectListItem>();

            var light = new SelectListItem {Text = "Light", Value = "light"};
            var dark = new SelectListItem {Text = "Dark", Value = "dark"};

            output.Add(light);
            output.Add(dark);

            return output;
        }

        public static List<SelectListItem> MatchTypesList(this HtmlHelper helper)
        {
            return FromEnum<MatchTypes>();
        }

        public static List<SelectListItem> DateFormatTypesList(this HtmlHelper helper)
        {
            return FromEnum<DateFormatTypes>();
        }

        private static List<SelectListItem> FromEnum<T>()
        {
            var items = from object match in Enum.GetValues(typeof (T))
                        let value = (int) match
                        select
                            new SelectListItem
                                {
                                    Text = GetDescription((Enum) match),
                                    Value = Enum.GetName(typeof(T), value)
                                };

            return items.ToList();
        }

        private static string GetDescription(Enum en)
        {
            var type = en.GetType();
            var memInfo = type.GetMember(en.ToString());

            if (memInfo.Length > 0)
            {
                var attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attrs.Length > 0)
                {
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }

            return en.ToString();
        }
    }
}
