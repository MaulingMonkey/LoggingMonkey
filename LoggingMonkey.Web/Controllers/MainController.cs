using System;
using System.Collections.Generic;
using System.Linq;
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
        [HttpGet]
        [Whitelisted]
        public ActionResult Index([FromUri] SearchModel model)
        {
            var messages = MessageRetriever.Get(model);
            var vm       = new IndexViewModel { Search = model, Messages = messages };

            return View(vm);
        }

        [HttpPost]
        [Whitelisted]
        public ActionResult UpdateDisplayOptions(DisplayOptionsModel model)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public ActionResult Auth(string token)
        {
            var cookie = new HttpCookie(Program.AuthCookieName, token);
            cookie.Expires = DateTime.Now.AddYears(10);

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
