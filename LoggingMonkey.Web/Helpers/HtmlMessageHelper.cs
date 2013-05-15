using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LoggingMonkey.Web.Models;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace LoggingMonkey.Web.Helpers
{
    public static class HtmlMessageHelper
    {
        public static MvcHtmlString RenderHeading(this HtmlHelper helper, SearchModel search)
        {
            return new MvcHtmlString("<h2>Scouring through " + ChannelHelper.GetById(search.ChannelId) + "...</h2>");
        }

        public static MvcHtmlString RenderMessage(this HtmlHelper<IndexViewModel> helper, Message message)
        {
            switch (message.Type)
            {
                case FastLogReader.LineType.Join:
                    return helper.Partial("MessageTypes/Join", message);

                case FastLogReader.LineType.Quit:
                    return helper.Partial("MessageTypes/Quit", message);

                case FastLogReader.LineType.Message:
                    return helper.Partial("MessageTypes/Message", message);

                case FastLogReader.LineType.Meta:
                    return helper.Partial("MessageTypes/Meta", message);

                case FastLogReader.LineType.Action:
                    return helper.Partial("messageTypes/Action", message);

                default:
                    goto case FastLogReader.LineType.Meta;
            }
        }
    }
}