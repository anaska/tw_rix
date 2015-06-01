using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace login
{
    public class WebApiConfig
    {
        public static void Register (HttpConfiguration config)
        {
            config.Routes.MapHttpRoute("API Default", "api/{controller}/{id}",
                new { id = RouteParameter.Optional });
        }
    }
}