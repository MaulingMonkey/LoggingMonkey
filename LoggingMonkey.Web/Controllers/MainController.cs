using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Http;
using LoggingMonkey.Web.Models;

namespace LoggingMonkey.Web.Controllers
{
    using HttpGet  = System.Web.Mvc.HttpGetAttribute;
    using HttpPost = System.Web.Mvc.HttpPostAttribute;
    using LoggingMonkey.Web.Helpers;

    public class MainController : Controller
    {
        [HttpGet]
        public ActionResult Index([FromUri] SearchModel model)
        {
            var messages = MessageRetriever.Get(model);
            var vm       = new IndexViewModel { Search = model, Messages = messages };

            return View(vm);
        }

        [HttpPost]
        public ActionResult UpdateDisplayOptions(DisplayOptionsModel model)
        {
            throw new NotImplementedException();
        }
    }
}
