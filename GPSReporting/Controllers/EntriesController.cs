using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GPSReporting.Controllers
{
    public class EntriesController : Controller
    {
        // GET: Entries
        public ActionResult AddVehicle()
        {
            return View();
        }
    }
}