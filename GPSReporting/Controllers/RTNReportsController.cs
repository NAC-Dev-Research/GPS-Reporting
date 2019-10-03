using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GPSReporting.DAL;
using GPSReporting.Models;
using GPSReporting.Models.RTNReports;
using System.Threading.Tasks;

namespace GPSReporting.Controllers
{
    public class RTNReportsController : RTNReportsFunctionController
    {
        MGPSAPIEntities db = new MGPSAPIEntities();
        List<TripsReportRaw> TripReportRawList = new List<TripsReportRaw>();

        public ActionResult VehicleUsageHighLights()
        {
            #region Check if date and time is in right order
            QueryDetails queryDetails = (QueryDetails)Session["queryDetails"];

            DateTime? DateFrom = queryDetails.DateFrom;
            DateTime? DateTo = queryDetails.DateTo;
            int? StartTime = queryDetails.StartTime;
            int? EndTime = queryDetails.EndTime;

            //Checks if the date/time should be swap or not; if the other should be before the other
            CheckDateTime(ref DateFrom, ref DateTo, ref StartTime, ref EndTime);
            #endregion

            //Start on creatimg trip report
            TripReportRawList.Clear();
            Task SortByLocation = Task.Factory.StartNew(() => IdentifyTripsRTN(DateFrom, DateTo, TripReportRawList));
            Task.WaitAll(new[] { SortByLocation });
            Session["RTNTripReportList"] = TripReportRawList;



            ViewBag.ReportDate = "Start date" + DateFrom.Value.ToString("MM-dd-yyyy") + " - End Date: " + DateTo.Value.ToString("MM-dd-yyyy");
            return View(TripReportRawList);
        }
    }
}