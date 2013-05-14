using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace LoggingMonkey.Web.Helpers
{
    public static class HtmlListHelpers
    {
        public static List<SelectListItem> ChannelsList(this HtmlHelper helper)
        {
            return ChannelHelper.GetAll().Select(channel => new SelectListItem {Text = channel.Value, Value = channel.Key.ToString(CultureInfo.InvariantCulture)}).ToList();
        }

        public static List<SelectListItem> MatchTypesList(this HtmlHelper helper)
        {
            return (from object match in Enum.GetValues(typeof (MatchTypes)) let value = (int) match select new SelectListItem {Text = GetDescription((MatchTypes) match), Value = value.ToString(CultureInfo.InvariantCulture)}).ToList();
        }

        private static string GetDescription(Enum en)
        {
            Type type = en.GetType();

            MemberInfo[] memInfo = type.GetMember(en.ToString());

            if (memInfo != null && memInfo.Length > 0)
            {
                object[] attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attrs != null && attrs.Length > 0)
                {
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }

            return en.ToString();
        }
    }
}
