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
        public static MvcHtmlString RenderMessage(this HtmlHelper<LoggingMonkey.Web.Models.IndexViewModel> helper, Message message)
        {
            switch (message.Type)
            {
                case FastLogReader.LineType.Join:
                    return helper.Partial("MessageTypes/Join", message);

                case FastLogReader.LineType.Quit:
                    return helper.Partial("MessageTypes/Quit", message);

                case FastLogReader.LineType.Message:
                    return helper.Partial("MessageTypes/Message", message);

                default:
                    throw new NotImplementedException("That type of message cannot be rendered.");
            }
        }
    }
}