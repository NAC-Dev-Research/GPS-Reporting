using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GPSReporting.DAL;
using GPSReporting.Models;
using GPSReporting.Models.RTNReports;
using System.Threading.Tasks;
using PagedList;

namespace GPSReporting.Controllers
{
    public class RTNReportsController : RTNReportsFunctionController
    {
        MGPSAPIEntities db = new MGPSAPIEntities();
        List<TripsReportRaw> TripReportRawList = new List<TripsReportRaw>();

        List<VehicleUsage> VehicleUsageSummary = new List<VehicleUsage>();
        List<UsageByTimeRange> VUByTimeRangeSummary = new List<UsageByTimeRange>();
        VehicleUsageHighlights VUHighlights = new VehicleUsageHighlights();
        NonCompliantHighlights NCHighlights = new NonCompliantHighlights();

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

            if (Session["RTNVUHighlights"] == null)
            {

                //Start on creating trip report
                TripReportRawList.Clear();
                Task CreateTrips = Task.Factory.StartNew(() => IdentifyTrips(DateFrom, DateTo, StartTime, EndTime, ref TripReportRawList));
                Task.WaitAll(new[] { CreateTrips });
                Session["RTNTripReportList"] = TripReportRawList;

                //Get vehicle usage summary
                Task CreateVehicleUsage = Task.Factory.StartNew(() => IdentifyVehicleUsage(TripReportRawList, ref VehicleUsageSummary));
                Task.WaitAll(new[] { CreateVehicleUsage });
                Session["RTNVehicleUsageSummary"] = VehicleUsageSummary;

                //Get vehicle usage by time range summary
                Task CreateByTimeRange = Task.Factory.StartNew(() => IdentifyVUByTimeRange(TripReportRawList, ref VUByTimeRangeSummary));
                Task.WaitAll(new[] { CreateByTimeRange });
                Session["RTNVUTimeRangeSummary"] = VUByTimeRangeSummary;

                //Get vehicle usage highlights
                TimeSpan days = (DateTime)DateTo - (DateTime)DateFrom;
                int daysInBetween = ((int)days.TotalDays) + 1;
                Task CreateVUHighlights = Task.Factory.StartNew(() => IdentifyVUHighlights(TripReportRawList, daysInBetween, ref VUHighlights));
                Session["RTNVUHighlights"] = VUHighlights;
            }
            else
                VUHighlights = (VehicleUsageHighlights)Session["RTNVUHighlights"];

            ViewBag.ReportDate = "Start date: " + DateFrom.Value.ToString("MM-dd-yyyy") + " - End Date: " + DateTo.Value.ToString("MM-dd-yyyy");
            return View(VUHighlights);
        }

        public ActionResult VehicleUsageDetails()
        {
            return View();
        }

        public ActionResult KeyTripsHighlights()
        {
            return View();
        }

        public ActionResult KeyTripsDetails()
        {
            return View();
        }

        public ActionResult NonCompliantHighlights()
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

            if (Session["RTNIdlingList"] == null)
            {
                List<ExcessiveIdling_vw> IdlingListDB = new List<ExcessiveIdling_vw>();
                List<SpeedViolation_vw> SpeedingListDB = new List<SpeedViolation_vw>();

                IdlingListDB = db.ExcessiveIdling_vw.Where(s => s.ReportDateFrom >= DateFrom && s.ReportDateFrom <= DateTo && s.Site == "RTN").OrderBy(s => s.Header).ToList();
                SpeedingListDB = db.SpeedViolation_vw.Where(s => s.ReportDateFrom >= DateFrom && s.ReportDateFrom <= DateTo && s.Site == "RTN").OrderBy(s => s.PlateNumber).ToList();

                //Identify idling violations
                Task CreateExcessiveIdling = Task.Factory.StartNew(() => IdentifyIdlingViolations(DateFrom, DateTo, StartTime, EndTime, IdlingListDB, ref NCHighlights));
                Task.WaitAll(new[] { CreateExcessiveIdling });

                //Identify speeding violations
                Task CreateOverSpeeding = Task.Factory.StartNew(() => IdentifySpeedingViolations(DateFrom, DateTo, StartTime, EndTime, SpeedingListDB, ref NCHighlights));
                Task.WaitAll(new[] { CreateOverSpeeding });

                Session["RTNIdlingList"] = NCHighlights;
            }
            else
                NCHighlights = (NonCompliantHighlights)Session["RTNIdlingList"];

            ViewBag.ReportDate = "Start date: " + DateFrom.Value.ToString("MM-dd-yyyy") + " - End Date: " + DateTo.Value.ToString("MM-dd-yyyy");
            return View(NCHighlights);
        }

        public ActionResult ExcessiveIdlingDetails(int? page, string chosenVehicle, string sortData)
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

            List<ExcessiveIdling_vw> IdlingListDB = new List<ExcessiveIdling_vw>();
            IdlingListDB = db.ExcessiveIdling_vw.Where(s => s.ReportDateFrom >= DateFrom && s.ReportDateFrom <= DateTo && s.Site == "RTN").OrderBy(s => s.Header).ToList();

            if (sortData == "Yes")
            {
                if (chosenVehicle.ToUpper() != "ALL")
                    IdlingListDB = db.ExcessiveIdling_vw.Where(s => (s.ReportDateFrom >= DateFrom && s.ReportDateFrom <= DateTo && s.Site == "RTN") && s.Header == chosenVehicle.Replace(System.Environment.NewLine, "")).OrderBy(s => s.Header).ToList();
                else
                    IdlingListDB = db.ExcessiveIdling_vw.Where(s => s.ReportDateFrom >= DateFrom && s.ReportDateFrom <= DateTo && s.Site == "RTN").OrderBy(s => s.Header).ToList();
            }

            List<RTNVehicleList> RTNVehicles = db.RTNVehicleLists.OrderBy(s => s.EquipmentID).ToList();
            ViewBag.RTNVehiclesList = new SelectList(RTNVehicles, "EquipmentID", "VehicleModel");

            int pageSize = 15;
            int pageNumber = (page ?? 1);

            return View(IdlingListDB.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult OverSpeedingDetails(int? page, string chosenVehicle, string sortData)
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

            List<SpeedViolation_vw> SpeedingListDB = new List<SpeedViolation_vw>();
            SpeedingListDB = db.SpeedViolation_vw.Where(s => s.ReportDateFrom >= DateFrom && s.ReportDateFrom <= DateTo && s.Site == "RTN").OrderBy(s => s.PlateNumber).ToList();

            if (sortData == "Yes")
            {
                if (chosenVehicle.ToUpper() != "ALL")
                    SpeedingListDB = db.SpeedViolation_vw.Where(s => (s.ReportDateFrom >= DateFrom && s.ReportDateFrom <= DateTo && s.Site == "RTN") && s.PlateNumber == chosenVehicle.Replace(System.Environment.NewLine, "")).OrderBy(s => s.PlateNumber).ToList();
                else
                    SpeedingListDB = db.SpeedViolation_vw.Where(s => s.ReportDateFrom >= DateFrom && s.ReportDateFrom <= DateTo && s.Site == "RTN").OrderBy(s => s.PlateNumber).ToList();
            }

            List<RTNVehicleList> RTNVehicles = db.RTNVehicleLists.OrderBy(s => s.EquipmentID).ToList();
            ViewBag.RTNVehiclesList = new SelectList(RTNVehicles, "EquipmentID", "VehicleModel");

            int pageSize = 15;
            int pageNumber = (page ?? 1);

            return View(SpeedingListDB.ToPagedList(pageNumber, pageSize));
        }
    }
}