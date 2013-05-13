using System.Web.Mvc;
using System.Web.Routing;

namespace LoggingMonkey.Web.Helpers
{
    public class WhitelistedAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Cookie2AccessControlStatus.Convert(filterContext.HttpContext) != AccessControlStatus.Whitelisted)
            {
                var redirectTargetDictionary = new RouteValueDictionary
                {
                    {"action", "Denied"},
                    {"controller", "Main"}
                };

                filterContext.Result = new RedirectToRouteResult(redirectTargetDictionary);
            }

            base.OnActionExecuting(filterContext);
        }
    }
}