using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PortfolioTracker.Controllers
{
    public class AnalyticsController : Controller
    {
        // GET: Analytics
        public ActionResult Graphs()
        {
            return View();
        }
    }
}