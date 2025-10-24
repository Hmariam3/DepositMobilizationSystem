using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace TRMS
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
        protected void Application_BeginRequest()
        {
            HttpContext.Current.Response.Headers.Add("Content-Security-Policy",
             "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';");
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1));
            Response.Cache.SetNoStore();

            // Add the X-Content-Type-Options header
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            // Remove the X-AspNet-Version header from the response
            Response.Headers.Remove("X-AspNet-Version");
            // Remove X-Powered-By header
            Response.Headers.Remove("X-Powered-By");
            // Remove the x-sourcefiles header as early as possible
            Response.Headers.Remove("X-sourcefiles");
            //Response.Headers.Add("Content-Security-Policy",
            //     "default-src 'self' http://10.9.218.218; " +
            //     "script-src 'self' 'unsafe-inline' 'unsafe-eval' http://10.9.218.218; " +
            //     "style-src 'self' 'unsafe-inline' http://10.9.218.218; " +
            //     "img-src 'self' data: http://10.9.218.218; " +
            //     "font-src 'self' http://10.9.218.218; " +
            //     "connect-src 'self' http://10.9.218.218; " +
            //     "object-src 'none'; " +
            //     "base-uri 'self'; " +
            //     "form-action 'self'; " +
            //     "frame-ancestors 'none'; " +
            //     "block-all-mixed-content;");
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1));
            Response.Cache.SetNoStore();
            if (Response.Cookies["MyCookie"] != null)
            {
                Response.Cookies["MyCookie"].Secure = false; // Matches requireSSL="false"
                Response.Cookies["MyCookie"].HttpOnly = true; // Matches httpOnlyCookies="true"
            }

            var cookie = Response.Cookies["MyCookie"];

            if (cookie != null)
            {
                // Set HttpOnly and Secure flags if necessary
                cookie.HttpOnly = true;
                cookie.Secure = false; // Change to true if using HTTPS
                cookie.Expires = DateTime.Now.AddDays(1); // Optional: Set expiration if needed

                // Manually add the SameSite attribute to the Set-Cookie header
                var cookieHeader = cookie.Name + "=" + cookie.Value + "; Path=" + cookie.Path + "; SameSite=Lax";

            }
        }

        protected void Application_PreSendRequestHeaders()
        {
            HttpContext.Current.Response.Headers.Remove("Server");
            Response.Headers.Remove("X-AspNet-Version");
            Response.Headers.Remove("X-AspNetMvc-Version");
            Response.Headers.Remove("X-SourceFiles");
            Response.Headers.Remove("X-Content-Type-Options");
            Response.Headers.Remove("X-Frame-Options");
            // Remove the x-sourcefiles header as early as possible
            Response.Headers.Remove("X-sourcefiles");
            // Removing headers
            //Response.Headers.Remove("Content-Security-Policy");
        }
        

    }
}
