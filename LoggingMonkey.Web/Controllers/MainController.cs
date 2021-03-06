﻿using System;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Http;
using LoggingMonkey.Web.Helpers;
using LoggingMonkey.Web.Models;

namespace LoggingMonkey.Web.Controllers
{
    using HttpGet  = System.Web.Mvc.HttpGetAttribute;
    using HttpPost = System.Web.Mvc.HttpPostAttribute;

    public class MainController : Controller
    {
        private DateTime? ConvertQueryValueToDate(string qs)
        {
            var dateString = Request.QueryString[qs];

            if (dateString == null)
            {
                return null;
            }

            DateTime date;

            var successful = DateTime.TryParse(dateString, out date) ||
                             DateTime.TryParseExact(dateString, "M/d h:mm tt", null, DateTimeStyles.None, out date);

            return successful ? date : (DateTime?)null;
        }

        [HttpGet]
        //[Whitelisted]
        public void Index([FromUri] SearchModel model)
        {
            Response.Buffer = false;
            Response.BufferOutput = false;

            var displayOptions = DisplayOptionsModel.FromHttpContext(HttpContext);

            model.FromDate = ConvertQueryValueToDate("FromDate");
            model.ToDate   = ConvertQueryValueToDate("ToDate");

            var messages = MessageRetriever.Get(model);
            var vm       = new IndexViewModel { Search = model, DisplayOptions = displayOptions, Messages = messages };

            UnbufferedRenderer.Render(ControllerContext, ViewData, TempData, Response, vm);
        }

        [HttpPost]
        public ActionResult UpdateDisplayOptions(DisplayOptionsModel model)
        {
            var cookie = new HttpCookie("LoggingMonkeyDisplay", DisplayOptionsModel.ToJson(model));
            Response.Cookies.Set(cookie);

            return Request.UrlReferrer != null ? (ActionResult)Redirect(Request.UrlReferrer.AbsoluteUri) : RedirectToAction("Index");
        }

        [HttpGet]
        [Whitelisted]
        public ActionResult Backup()
        {
            Stream zip;

            try
            {
                zip = System.IO.File.Open(Paths.BackupZip, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return new HttpStatusCodeResult(HttpStatusCode.ServiceUnavailable);
            }

            using (zip)
            {
                using (var package = Package.Open(zip, FileMode.Create))
                {
                    var files = Directory.GetFiles(Paths.LogsDirectory, "*.log", SearchOption.TopDirectoryOnly);

                    foreach (var file in files)
                    {
                        var relFile = Uri.EscapeDataString(Path.GetFileName(file));
                        var uri = PackUriHelper.CreatePartUri(new Uri(relFile, UriKind.Relative));
                        var part = package.CreatePart(uri, System.Net.Mime.MediaTypeNames.Text.Plain,
                                                      CompressionOption.Maximum);
                        
                        using (var fstream = System.IO.File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            using (var partstream = part.GetStream())
                            {
                                fstream.CopyTo(partstream);
                            }
                        }

                        package.Flush();
                    }

                    package.Close();
                }

                zip.Flush();
                zip.Position = 0;

                var name = String.Format("LoggingMonkey Backup {0}.zip", DateTime.Now.ToString("M-d-yy h.mm tt"));

                return new FilePathResult(Paths.BackupZip, "applicaton/zip") {FileDownloadName = name};
            }
        }

        [HttpGet]
        public ActionResult Auth(string token)
        {
            var cookie = new HttpCookie(Program.AuthCookieName, token) {Expires = DateTime.Now.AddYears(10)};

            Response.Cookies.Add(cookie);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Denied()
        {
            switch (Cookie2AccessControlStatus.Convert(HttpContext))
            {
                case AccessControlStatus.Blacklisted:
                    ViewBag.Message = "Your account is blacklisted.";
                    break;

                case AccessControlStatus.Pending:
                    ViewBag.Message = "Pending whitelist approval.";
                    break;

                case AccessControlStatus.Error:
                    ViewBag.Message = "An unknown error occured.";
                    break;

                case AccessControlStatus.Whitelisted:
                    ViewBag.Message = "You shouldn't be here.";
                    break;
            }

            return View();
        }
    }
}
