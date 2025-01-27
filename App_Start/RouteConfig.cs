using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace PortfolioTracker
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Portfolio", action = "Index", id = UrlParameter.Optional }
            );
            routes.MapRoute(
                 name: "About",
                 url: "About",
                 defaults: new { controller = "Portfolio", action = "Full" }
);
            routes.MapRoute(
                 name: "Alerts",
                 url: "Alerts",
                 defaults: new { controller = "Portfolio", action = "Alerts" }
);


        }
    }
}
