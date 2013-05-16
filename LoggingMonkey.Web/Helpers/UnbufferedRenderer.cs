using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LoggingMonkey.Web.Models;

namespace LoggingMonkey.Web.Helpers
{
    public class UnbufferedRenderer
    {
        protected static string RenderPartialViewToString(ControllerContext context, ViewDataDictionary viewData, TempDataDictionary tempData, string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = context.RouteData.GetRequiredString("action");

            viewData.Model = model;

            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(context, viewName);
                var viewContext = new ViewContext(context, viewResult.View, viewData, tempData, sw);
                viewResult.View.Render(viewContext, sw);

                return sw.GetStringBuilder().ToString();
            }
        }

        public static void Render(ControllerContext context, ViewDataDictionary viewData, TempDataDictionary tempData, HttpResponseBase response, IndexViewModel model)
        {
            RenderHeader(context, viewData, tempData, response, model);
            RenderPreMessages(context, viewData, tempData, response, model);

            foreach (var message in model.Messages.Messages)
            {
                switch (message.Type)
                {
                    case FastLogReader.LineType.Join:
                        response.Write(RenderPartialViewToString(context, viewData, tempData, "~/Views/Unbuffered/MessageTypes/Join.cshtml", message));
                        break;

                    case FastLogReader.LineType.Quit:
                        response.Write(RenderPartialViewToString(context, viewData, tempData, "~/Views/Unbuffered/MessageTypes/Quit.cshtml", message));
                        break;

                    case FastLogReader.LineType.Message:
                        response.Write(RenderPartialViewToString(context, viewData, tempData, "~/Views/Unbuffered/MessageTypes/Message.cshtml", message));
                        break;

                    case FastLogReader.LineType.Meta:
                        response.Write(RenderPartialViewToString(context, viewData, tempData, "~/Views/Unbuffered/MessageTypes/Meta.cshtml", message));
                        break;

                    case FastLogReader.LineType.Action:
                        response.Write(RenderPartialViewToString(context, viewData, tempData, "~/Views/Unbuffered/MessageTypes/Action.cshtml", message));
                        break;

                    default:
                        goto case FastLogReader.LineType.Meta;
                }
            }

            RenderPostMessages(context, viewData, tempData, response, model);
            RenderFooter(context, viewData, tempData, response, model);
        }

        private static void RenderHeader(ControllerContext context, ViewDataDictionary viewData, TempDataDictionary tempData, HttpResponseBase response, IndexViewModel model)
        {
            response.Write(RenderPartialViewToString(context, viewData, tempData, "~/Views/Unbuffered/Header.cshtml", model));
        }

        private static void RenderPreMessages(ControllerContext context, ViewDataDictionary viewData, TempDataDictionary tempData, HttpResponseBase response, IndexViewModel model)
        {
            response.Write(RenderPartialViewToString(context, viewData, tempData, "~/Views/Unbuffered/PreMessages.cshtml", model));
        }

        private static void RenderPostMessages(ControllerContext context, ViewDataDictionary viewData, TempDataDictionary tempData, HttpResponseBase response, IndexViewModel model)
        {
            response.Write(RenderPartialViewToString(context, viewData, tempData, "~/Views/Unbuffered/PostMessages.cshtml", model));
        }

        private static void RenderFooter(ControllerContext context, ViewDataDictionary viewData, TempDataDictionary tempData, HttpResponseBase response, IndexViewModel model)
        {
            response.Write(RenderPartialViewToString(context, viewData, tempData, "~/Views/Unbuffered/Footer.cshtml", model));
        }
    }
}