using System.Net;
using System.Web;

namespace LoggingMonkey.Web.Helpers
{
    public static class Cookie2AccessControlStatus
    {
        public static AccessControlStatus Convert(HttpContextBase context)
        {
            // Determine auth
            var acs = AccessControlStatus.Error;

            foreach (var k in context.Request.Cookies)
            {
                var cookie = context.Request.Cookies[k.ToString()];

                if (cookie.Name == Program.AuthCookieName)
                {
                    acs = AccessControl.GetStatus(cookie.Value);
                    break;
                }
            }
            
            return acs;
        }
    }
}