using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using GPSReporting.Models;
using System.Drawing;
using GPSReporting.DAL;
using System.Threading.Tasks;
using System.Threading;
using PagedList;

namespace GPSReporting.Controllers
{
    public class ReportsController : Controller
    {
        MGPSAPIEntities db = new MGPSAPIEntities();
        ReportsViewModel viewModel = new ReportsViewModel();
        List<TripsReportRaw> TripReportRawList = new List<TripsReportRaw>();

        public ActionResult Login(string username, string password, string loginbtn)
        {
            if (loginbtn == "YES" && username != "" && password != "")
            {
                if (Session["user"] == null)
                {
                    var user = db.UserLists.Where(s => s.Username == username && s.Password == password).FirstOrDefault();
                    if (user != null)
                    {
                        viewModel.CurrentUser = user.Username;
                        viewModel.UserDetails = user;
                        Session["user"] = user.Username;
                        ViewBag.CurrentUser = System.Web.HttpContext.Current.Session["user"];
                        ViewBag.LogInSuccess = "Success";

                        LogList tempLog = new LogList();
                        tempLog.Username = user.Username;
                        tempLog.FirstName = user.FirstName;
                        tempLog.LastName = user.LastName;
                        tempLog.Date = System.DateTime.Today.Date;
                        tempLog.Day = System.DateTime.Today.DayOfWeek.ToString();
                        tempLog.Login = System.DateTimeOffset.Now.TimeOfDay.ToString().Substring(0, 8);
                        tempLog.Details = "logged in GPS Reporting";

                        viewModel.LogDetails = tempLog;
                        db.LogLists.Add(tempLog);
                        db.SaveChanges();
                                                
                        Session["mySession"] = viewModel;
                        Session["role"] = user.Role;
                        return RedirectToAction("Index");
                    }
                    else
                        ViewBag.LogInSuccess = "Fail";
                }
                else
                {
                    var user = db.UserLists.Where(s => s.Username == username && s.Password == password).FirstOrDefault();
                    if (user != null)
                    {
                        Session["mySession"] = viewModel;
                        return RedirectToAction("Index");
                    }
                }
            }
            Session["mySession"] = viewModel;
            return View();
        }

        public ActionResult Index(string showReport, DateTime? dateFrom, DateTime? dateTo, int? startTime, int? endTime, string site, string vehicleType)
        {
            if (Session["user"] == null)
                Response.Redirect("Login", true);
            
            viewModel = (ReportsViewModel)Session["mySession"];
            Session["chosenSite"] = site;
            Session["chosenVehicleType"] = vehicleType;
            ViewBag.ShowReport = "HIDE";
            ViewBag.Continue = "TRUE";

            if (showReport == "YES" && Session["user"] != null)
            {
                if (dateFrom > dateTo)
                {
                    DateTime? _tempDate = dateFrom;
                    dateFrom = dateTo;
                    dateTo = _tempDate;
                }
                if (startTime == null)
                    startTime = 0;
                if (endTime == null)
                    endTime = 24;
                if (startTime > endTime)
                {
                    int? tempTime = startTime;
                    startTime = endTime;
                    endTime = tempTime;
                }
                viewModel.StartTime = startTime;
                viewModel.EndTime = endTime;
                viewModel.DateFROM = dateFrom;
                viewModel.DateTO = dateTo;

                viewModel._tripReportRaw.Clear();
                Task SortByLocation = Task.Factory.StartNew(() => IdentifyTrips(dateFrom, dateTo));
                Task.WaitAll(new[] { SortByLocation });

                viewModel.NoncompliantHighlights = new NonCompliantHighlights();
                viewModel._excessiveIdlingList.Clear();
                viewModel._excessiveIdlingListCopy.Clear();
                viewModel._overSpeedingList.Clear();
                viewModel._overSpeedingListCopy.Clear();

                Task SortByIdlingViolations = Task.Factory.StartNew(() => IdentifyIdlingViolations(site));
                Task.WaitAll(new[] { SortByIdlingViolations });

                Task SortBySpeedingViolations = Task.Factory.StartNew(() => IdentifySpeedingViolations(site));
                Task SortBySelectedTime = Task.Factory.StartNew(() => IdentifyTripsBySelectedTime());
                Task.WaitAll(new[] { SortBySelectedTime, SortBySpeedingViolations });

                viewModel._vehicleUsageList.Clear();
                viewModel._usageByTimeRangeList.Clear();
                viewModel._keyTripsList.Clear();
                viewModel._keyTripsListCopy.Clear();
                viewModel._DMCVehicleList.Clear();
                viewModel._DMCVehicleNotUsedList.Clear();
                viewModel.UsageHighlights = new VehicleUsageHighlights();
                viewModel.KeyTripsHighlights = new KeyTripsHighlights();

                Task SortByVehicleNo = Task.Factory.StartNew(() => IdentifyVehicleUsage(dateFrom, dateTo));
                Task SortByTimeRange = Task.Factory.StartNew(() => IdentifyUsageByTimeRange());
                Task.WaitAll(new[] { SortByVehicleNo, SortByTimeRange });

                Task SortByKeyTrips = Task.Factory.StartNew(() => IdentifyKeyTrips());
                Task.WaitAll(new[] { SortByKeyTrips });

                int NoOfDaysInBetween = ((TimeSpan)(dateTo - dateFrom)).Days;
                ViewBag.ShowReport = "USAGE";
                viewModel.DaysInBetween = NoOfDaysInBetween;
                if (viewModel.CurrentWindow == null)
                    viewModel.CurrentWindow = "USAGE";
                else
                    ViewBag.ShowReport = viewModel.CurrentWindow;

                if (viewModel._tripReportRaw.Count() <= 0)
                    ViewBag.Continue = "FALSE";
                else
                {
                    //Session["mySession"] = viewModel;
                    return RedirectToAction("SummaryPage");
                }
            }

            return View(viewModel);
        }

        public ActionResult SummaryPage(string reportType, string detailsWindow, string backToInput, string logOut, 
                                        string vehicleSummary, string hideVehicleSummary, string keyTripsSummary, 
                                        string hideKeyTripsSummary, string speedingV, string idlingV)
        {
            if (Session["user"] == null)
                Response.Redirect("Login", true);

            viewModel = (ReportsViewModel)Session["mySession"];
            ViewBag.ReportDateRange = "Report from " + viewModel.DateFROM.Value.ToString("MM-dd-yyyy") + " to " + viewModel.DateTO.Value.ToString("MM-dd-yyyy") + " (" + viewModel.DaysInBetween + " days)";

            if (detailsWindow == "YES" && viewModel.CurrentWindow == "KEYTRIPS")
                return RedirectToAction("KeyTripsDetails"); // Frequently Made Key Trips
            if (detailsWindow == "YES" && viewModel.CurrentWindow == "EVENTS")
                return RedirectToAction("NoncompliantsDetails"); //Noncompliant Events window
            if (detailsWindow == "YES")
                return RedirectToAction("DetailsWindow"); //Vehicle Usage window
            if (backToInput == "YES")
                return RedirectToAction("Index");

            if (vehicleSummary == "YES")
                ViewBag.ShowVehicleSummary = "YES";
            if (hideVehicleSummary == "YES")
                ViewBag.ShowVehicleSummary = "NO";

            if (keyTripsSummary == "YES")
                ViewBag.ShowKeyTripsSummary = "YES";
            if (hideKeyTripsSummary == "YES")
                ViewBag.ShowKeyTripsSummary = "NO";

            if (speedingV == "YES")
                ViewBag.ShowIdling = "NO";
            if (idlingV == "YES")
                ViewBag.ShowIdling = "YES";

            if (Session["user"] != null)
            {
                if (reportType == "USAGE")
                    viewModel.CurrentWindow = "USAGE";
                if (reportType == "KEYTRIPS")
                    viewModel.CurrentWindow = "KEYTRIPS";
                if (reportType == "EVENTS")
                    viewModel.CurrentWindow = "EVENTS";
                if (reportType == "PRODUCTIVITY")
                    viewModel.CurrentWindow = "PRODUCTIVITY";
            }

            if (logOut == "YES")
            {
                var log = db.LogLists.Where(s => s.Username == viewModel.LogDetails.Username &&
                                                s.Date == viewModel.LogDetails.Date &&
                                                s.Login == viewModel.LogDetails.Login).FirstOrDefault();
                LogList updatedLog = new LogList();
                updatedLog = log;
                updatedLog.Logout = System.DateTimeOffset.Now.TimeOfDay.ToString().Substring(0, 8);
                db.Entry(updatedLog).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                Session["user"] = null;
                Session.Clear();
                return RedirectToAction("Login");
            }

            return View(viewModel);
        }

        public ActionResult Account(string apply, string curPassword, string newPassword, string reenterPassword)
        {
            if (Session["user"] == null)
                Response.Redirect("Login", true);

            viewModel = (ReportsViewModel)Session["mySession"];
            UserList tempUser = viewModel.UserDetails;
            System.Diagnostics.Debug.WriteLine("User is: " + tempUser);

            if (apply == "YES")
            {
                var userDB = db.UserLists.Where(s => s.Username == viewModel.UserDetails.Username && s.Password == curPassword).FirstOrDefault();
                if (userDB != null)
                {
                    if (newPassword == reenterPassword)
                    {
                        var id = db.UserLists.Find(userDB.ID);
                        UserList updatedUser = new UserList();
                        updatedUser = userDB;
                        updatedUser.Password = newPassword;
                        db.Entry(updatedUser).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();

                        Thread.Sleep(2000);
                        Session["user"] = null;
                        Session.Clear();
                        return RedirectToAction("Login", "Reports");
                    }
                    else
                        ViewBag.Notif = "New password and re-entered password don't match. Please make sure both are the same.";
                }
                else
                    ViewBag.Notif = "Current password is incorrect. Please make sure password is correct.";
            }

            return View(tempUser);
        }

        public ActionResult Log()
        {
            if (Session["user"] == null)
                Response.Redirect("Login", true);

            List<LogList> curList = new List<LogList>();
            curList = db.LogLists.OrderByDescending(s => s.Date).ToList();

            return View(curList);
        }

        public ActionResult KeyTripsDetails(int? page, string backToMenu, string chosenVehicle, string sortData)
        {
            if (Session["user"] == null)
                Response.Redirect("Login", true);

            viewModel = (ReportsViewModel)Session["mySession"];
            List<KeyTrips> keyTrips = new List<KeyTrips>();
            List<DMCVehicleList> DMCVehicles = db.DMCVehicleLists.OrderBy(s => s.PlateNumber).ToList();
            ViewBag.DMCVehiclesList = new SelectList(DMCVehicles, "PlateNumber", "VehicleModel");

            if (sortData == "Yes")
            {
                if (chosenVehicle != "ALL")
                    viewModel._keyTripsListCopy = viewModel._keyTripsList.Where(s => s.VehicleID == chosenVehicle).OrderBy(s => s.VehicleID).ThenBy(s => s.StartLocation).ThenBy(s => s.Date).ToList();
                else
                    viewModel._keyTripsListCopy = viewModel._keyTripsList.OrderBy(s => s.VehicleID).ThenBy(s => s.StartLocation).ThenBy(s => s.Date).ToList();
            }
            keyTrips = viewModel._keyTripsListCopy.OrderBy(s => s.VehicleID).ThenBy(s => s.StartLocation).ThenBy(s => s.Date).ToList();

            if (backToMenu == "Yes")
                return RedirectToAction("SummaryPage");
            
            int pageSize = 15;
            int pageNumber = (page ?? 1);

            return View(keyTrips.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult KeyTripsBreakdown(int? entryID, string backToMenu)
        {
            if (Session["user"] == null)
                Response.Redirect("Login", true);

            viewModel = (ReportsViewModel)Session["mySession"];

            if (backToMenu == "Yes")
                return RedirectToAction("KeyTripsDetails");

            var curDataList = new List<TripsReportRaw>();
            KeyTrips KeyTripBasis = new KeyTrips();

            KeyTripBasis = viewModel._keyTripsList.Where(s => s.EntryID == entryID).FirstOrDefault();
            curDataList = viewModel._tripReportRaw.Where(s => s.EquipmentNo == KeyTripBasis.VehicleID && s.StartLocation == KeyTripBasis.StartLocation &&
                                                                s.EndLocation == KeyTripBasis.EndLocation).ToList();

            return View(curDataList);
        }

        public ActionResult NoncompliantsDetails(int? page, string backToMenu, string idling, string chosenVehicle, string sortData)
        {
            if (Session["user"] == null)
                Response.Redirect("Login", true);

            viewModel = (ReportsViewModel)Session["mySession"];
            string sortBySite = (string)Session["chosenSite"];
            string sortByVehicleType = (string)Session["chosenVehicleType"];
            
            List<SpeedViolation_vw> speeding = new List<SpeedViolation_vw>();
            List<DMCVehicleList> DMCVehicles = db.DMCVehicleLists.OrderBy(s => s.PlateNumber).ToList();
            List<RTNVehicleList> RTNVehicles = db.RTNVehicleLists.OrderBy(s => s.EquipmentID).ToList();
            ViewBag.DMCVehiclesList = new SelectList(DMCVehicles, "PlateNumber", "VehicleModel");
            if (sortBySite == "RTN")
                ViewBag.DMCVehiclesList = new SelectList(RTNVehicles, "EquipmentID", "VehicleModel");

            if (sortData == "Yes")
            {
                if (chosenVehicle != "ALL")
                    viewModel._overSpeedingListCopy = viewModel._overSpeedingList.Where(s => s.EquipmentID == chosenVehicle.Replace(System.Environment.NewLine, "")).OrderBy(s => s.PlateNumber).ThenBy(s => s.ReportDateFrom).ToList();
                else
                    viewModel._overSpeedingListCopy = viewModel._overSpeedingList.OrderBy(s => s.PlateNumber).ThenBy(s => s.ReportDateFrom).ToList();
            }
            speeding = viewModel._overSpeedingListCopy.OrderBy(s => s.PlateNumber).ThenBy(s => s.ReportDateFrom).ToList();

            if (backToMenu == "Yes")
                return RedirectToAction("SummaryPage");
            if (idling == "Yes")
                return RedirectToAction("IdlingDetails");

            int pageSize = 15;
            int pageNumber = (page ?? 1);

            return View(speeding.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult IdlingDetails(int? page, string backToMenu, string overSpeed, string chosenVehicle, string sortData)
        {
            if (Session["user"] == null)
                Response.Redirect("Login", true);

            viewModel = (ReportsViewModel)Session["mySession"];
            string sortBySite = (string)Session["chosenSite"];
            List<ExcessiveIdling_vw> idling = new List<ExcessiveIdling_vw>();
            List<DMCVehicleList> DMCVehicles = db.DMCVehicleLists.OrderBy(s => s.PlateNumber).ToList();
            List<RTNVehicleList> RTNVehicles = db.RTNVehicleLists.OrderBy(s => s.EquipmentID).ToList();
            ViewBag.DMCVehiclesList = new SelectList(DMCVehicles, "PlateNumber", "VehicleModel");
            if (sortBySite == "RTN")
                ViewBag.DMCVehiclesList = new SelectList(RTNVehicles, "EquipmentID", "VehicleModel");

            if (sortData == "Yes")
            {
                if (chosenVehicle != "ALL")
                    viewModel._excessiveIdlingListCopy = viewModel._excessiveIdlingList.Where(s => s.Header == chosenVehicle.Replace(System.Environment.NewLine, "")).OrderBy(s => s.Header).ThenBy(s => s.ReportDateFrom).ToList();
                else
                    viewModel._excessiveIdlingListCopy = viewModel._excessiveIdlingList.OrderBy(s => s.Header).ThenBy(s => s.ReportDateFrom).ToList();
            }

            idling = viewModel._excessiveIdlingListCopy.OrderBy(s => s.Header).ThenBy(s => s.ReportDateFrom).ToList();

            if (backToMenu == "Yes")
                return RedirectToAction("SummaryPage");
            if (overSpeed == "Yes")
                return RedirectToAction("NoncompliantsDetails");

            int pageSize = 15;
            int pageNumber = (page ?? 1);

            return View(idling.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult VehicleUsageBreakdown(int? page, string vehicleID, string backToMenu)
        {
            if (Session["user"] == null)
                Response.Redirect("Login", true);

            viewModel = (ReportsViewModel)Session["mySession"];

            if (backToMenu == "Yes")
                return RedirectToAction("DetailsWindow");

            if (vehicleID != "" && vehicleID != null)
            {
                viewModel.VehicleID = vehicleID;
                viewModel._chosenVehicleUsage = viewModel._tripReportRaw.Where(s => s.EquipmentNo == viewModel.VehicleID).OrderBy(s => s.ReportDate).ToList();

                DMCVehicleList vehicle = db.DMCVehicleLists.Where(s => s.PlateNumber == vehicleID).SingleOrDefault();
                viewModel.PageTitle = "Vehicle usage breakdown for " + vehicle.VehicleModel + " with plate number " + vehicle.PlateNumber;
            }

            var curDataList = new List<TripsReportRaw>();
            curDataList = viewModel._chosenVehicleUsage;
            ViewBag.PageTitle = viewModel.PageTitle;

            int pageSize = 15;
            int pageNumber = (page ?? 1);

            return View(curDataList.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult DetailsWindow(string backToMenu)
        {
            if (Session["user"] == null)
                Response.Redirect("Login", true);

            viewModel = (ReportsViewModel)Session["mySession"];

            if (backToMenu == "Yes")
                return RedirectToAction("SummaryPage");
            System.Diagnostics.Debug.WriteLine("Current Window: " + viewModel.CurrentWindow + " | Date From: " + viewModel.DateFROM + " |  Date To: " + viewModel.DateTO);

            return View(viewModel);
        }

        public void IdentifyKeyTrips()
        {
            viewModel = (ReportsViewModel)Session["mySession"];
            List<TripsReportRaw> TripListByTrips = viewModel._tripReportRaw.OrderBy(s => s.StartLocation).ThenBy(s => s.EndLocation).ToList();

            if (TripListByTrips.Count() != 0)
            {
                int _tripCount = 0;
                int _cycleCount = 0;
                int _routeCount = 0;
                string _prevVehicle = "";
                decimal _totalTravelTime = 0;
                decimal? _totalidlingTime = 0;
                decimal? _totalStopTime = 0;
                decimal _routeTotalTravelTime = 0;
                string _prevEndLocation = "";
                string _prevStartLocation = "";
                string _routeMostVisited = "";
                int _routeNoOfTrips = 0;
                decimal _routeAverageTravelTime = 0;
                string _vehicleMostTrips = "";
                int _vehicleNoOfTrips = 0;
                int _entryID = 0;

                foreach (var data in TripListByTrips)
                {
                    _cycleCount++;
                    if (_prevEndLocation == "" && _prevStartLocation == "")
                    {
                        _prevStartLocation = data.StartLocation;
                        _prevEndLocation = data.EndLocation;
                        _prevVehicle = data.EquipmentNo;
                        _totalTravelTime = 0;
                        _totalidlingTime = _totalStopTime = 0;
                        _tripCount = 0;
                    }

                    if (data.StartLocation == _prevStartLocation && data.EndLocation == _prevEndLocation)
                    {
                        _routeCount++;
                        _routeTotalTravelTime += data.TripDurationDec;
                        if (data.EquipmentNo == _prevVehicle)
                        {
                            _totalTravelTime += data.TripDurationDec;
                            _totalidlingTime += data.TripIdlingTimeDec;
                            _totalStopTime += data.TripStopTimeDec;
                            _tripCount++;
                        }
                        if (_routeCount > _routeNoOfTrips)
                        {
                            _routeMostVisited = _prevStartLocation + " to " + _prevEndLocation;
                            _routeAverageTravelTime = _routeTotalTravelTime;
                            _routeNoOfTrips = _routeCount;
                            if (_vehicleMostTrips == "" || _vehicleMostTrips != data.EquipmentNo)
                            {
                                _vehicleMostTrips = data.EquipmentNo;
                                _vehicleNoOfTrips = 1;
                            }

                            if (_vehicleMostTrips == data.EquipmentNo && _tripCount > _vehicleNoOfTrips)
                            {
                                _vehicleMostTrips = data.EquipmentNo;
                                _vehicleNoOfTrips = _tripCount;
                            }
                        }
                    }
                    if (data.StartLocation != _prevStartLocation || data.EndLocation != _prevEndLocation)
                    {
                        _routeCount = 1;
                        _routeTotalTravelTime = data.TripDurationDec;
                    }

                    if (data.EquipmentNo != _prevVehicle || data.StartLocation != _prevStartLocation ||
                        data.EndLocation != _prevEndLocation || _cycleCount == TripListByTrips.Count())
                    {
                        if (_tripCount > 1)
                        {
                            _totalTravelTime = Math.Round((_totalTravelTime / _tripCount), 2);
                            _totalidlingTime = Math.Round((Convert.ToDecimal(_totalidlingTime) / _tripCount), 2);
                            _totalStopTime = Math.Round((Convert.ToDecimal(_totalStopTime) / _tripCount), 2);
                        }

                        KeyTrips curData = new KeyTrips();

                        curData.EntryID = _entryID;
                        curData.VehicleID = _prevVehicle;
                        curData.EndLocation = _prevEndLocation;
                        curData.StartLocation = _prevStartLocation;
                        curData.NumberOfTrips = Convert.ToInt32(_tripCount);
                        curData.AverageTravelTime = ConvertToTimeFormat(_totalTravelTime);
                        curData.AverageIdlingTime = ConvertToTimeFormat(Convert.ToDecimal(_totalidlingTime));
                        curData.AverageIdlingTimeDec = Convert.ToDecimal(_totalidlingTime);
                        curData.AverageStopTime = ConvertToTimeFormat(Convert.ToDecimal(_totalStopTime));
                        viewModel._keyTripsList.Add(curData);
                        viewModel._keyTripsListCopy.Add(curData);
                        _entryID++;

                        if (_cycleCount == TripListByTrips.Count() &&
                            ((data.StartLocation != _prevStartLocation || data.EndLocation != _prevEndLocation) || data.EquipmentNo != _prevVehicle))
                        {
                            curData = new KeyTrips();

                            curData.EntryID = _entryID;
                            curData.VehicleID = data.EquipmentNo;
                            curData.EndLocation = data.EndLocation;
                            curData.StartLocation = data.StartLocation;
                            curData.NumberOfTrips = 1;
                            curData.AverageTravelTime = ConvertToTimeFormat(data.TripDurationDec);
                            curData.AverageIdlingTime = ConvertToTimeFormat(Convert.ToDecimal(data.TripIdlingTimeDec));
                            curData.AverageIdlingTimeDec = Convert.ToDecimal(data.TripIdlingTimeDec);
                            curData.AverageStopTime = ConvertToTimeFormat(Convert.ToDecimal(data.TripStopTimeDec));
                            viewModel._keyTripsList.Add(curData);
                            viewModel._keyTripsListCopy.Add(curData);
                        }

                        _tripCount = 1;
                        _totalTravelTime = data.TripDurationDec;
                        _totalidlingTime = data.TripIdlingTimeDec;
                        _totalStopTime = data.TripStopTimeDec;
                        _prevVehicle = data.EquipmentNo;
                        _prevEndLocation = data.EndLocation;
                        _prevStartLocation = data.StartLocation;
                    }
                }

                _routeMostVisited = _routeMostVisited.Replace("[", "");
                _routeMostVisited = _routeMostVisited.Replace("]", "");

                string tempAveTravelTime = ConvertToHrMinFormat(_routeAverageTravelTime);
                if (_routeNoOfTrips > 0)
                    tempAveTravelTime = ConvertToHrMinFormat(_routeAverageTravelTime / _routeNoOfTrips);

                KeyTrips HighestIdling = new KeyTrips();
                string _routeIdlingTime = "";
                string _routeMostIdling = "";
                decimal highestValue = viewModel._keyTripsList.Max(s => s.AverageIdlingTimeDec);
                HighestIdling = viewModel._keyTripsList.Where(s => s.AverageIdlingTimeDec == highestValue).FirstOrDefault();

                _routeMostIdling = HighestIdling.StartLocation + " to " + HighestIdling.EndLocation;
                _routeMostIdling = _routeMostIdling.Replace("[", "");
                _routeMostIdling = _routeMostIdling.Replace("]", "");
                _routeIdlingTime = ConvertToHrMinFormat(HighestIdling.AverageIdlingTimeDec);

                decimal _vehicleIdlingTime = 0;
                _vehicleIdlingTime = TripListByTrips.Where(s => s.EquipmentNo == HighestIdling.VehicleID && s.StartLocation == HighestIdling.StartLocation &&
                                                            s.EndLocation == HighestIdling.EndLocation).Max(s => s.TripIdlingTimeDec).Value;

                string _VehicleIdlingTimeStr = ConvertToHrMinFormat(_vehicleIdlingTime);
                DMCVehicleList vehicleMostTripModel = db.DMCVehicleLists.Where(s => s.PlateNumber == _vehicleMostTrips).SingleOrDefault();
                DMCVehicleList vehicleMostidlingModel = db.DMCVehicleLists.Where(s => s.PlateNumber == HighestIdling.VehicleID).SingleOrDefault();

                viewModel.KeyTripsHighlights.RouteMostTrip = _routeMostVisited;
                viewModel.KeyTripsHighlights.RouteNoOfTrip = _routeNoOfTrips;
                viewModel.KeyTripsHighlights.AverageTravelTime = tempAveTravelTime;
                viewModel.KeyTripsHighlights.VehicleMostTrip = _vehicleMostTrips;
                viewModel.KeyTripsHighlights.VehicleMostTripModel = vehicleMostTripModel.VehicleModel;
                viewModel.KeyTripsHighlights.VehicleNoOfTrip = _vehicleNoOfTrips;
                viewModel.KeyTripsHighlights.RouteMostIdling = _routeMostIdling;
                viewModel.KeyTripsHighlights.RouteIdlingTime = _VehicleIdlingTimeStr;
                viewModel.KeyTripsHighlights.VehicleMostIdling = HighestIdling.VehicleID;
                viewModel.KeyTripsHighlights.VehicleMostIdlingModel = vehicleMostidlingModel.VehicleModel;
                viewModel.KeyTripsHighlights.VehicleIdlingTime = _routeIdlingTime;

                List<string> GeofencePOI = new List<string>()
                {
                "Nac Tower", "Palosapis", "Mine camp main", "Mine camp Annex 1", "Mine camp gate",
                "Uty. Runway/Beach Dinapigue", "Santiago Liaison Office", "MGB Region 2", "Uty. CTerminal Baler",
                "Uty. NE Supermarket Baler", "Ayod", "Bucal Norte", "Bucal Sur", "Dibulo", "Digumased"
                };
            }
        }

        public void IdentifyVehicleUsage(DateTime? dateFrom, DateTime? dateTo)
        {
            viewModel = (ReportsViewModel)Session["mySession"];
            List<TripsReportRaw> TripListByVehicleID = TripReportRawList.OrderBy(s => s.EquipmentNo).ToList();

            int _cycleCount = 0;
            int _numberOfTrips = 0;
            string curVehicleID = "";
            decimal _totalMileage = 0;
            decimal _averageMileage = 0;
            decimal _totalTravelTime = 0;
            decimal _averageTravelTime = 0;
            foreach (var data in TripListByVehicleID)
            {
                if (curVehicleID == "")
                    curVehicleID = data.EquipmentNo;
                _cycleCount += 1;

                if (curVehicleID != data.EquipmentNo || _cycleCount == TripListByVehicleID.Count())
                {
                    if (_cycleCount == TripListByVehicleID.Count() && curVehicleID == data.EquipmentNo)
                    {
                        _numberOfTrips += 1;
                        _totalMileage += data.TripLength;
                        _totalTravelTime += data.TripDurationDec;
                    }

                    DMCVehicleList curVehicle = db.DMCVehicleLists.Where(s => s.PlateNumber == curVehicleID).SingleOrDefault();
                    VehicleUsage curData = new VehicleUsage();

                    _averageMileage = Math.Round((_totalMileage / _numberOfTrips), 2);
                    _averageTravelTime = Math.Floor((_totalTravelTime / _numberOfTrips));

                    curData.VehicleID = curVehicleID;
                    curData.VehicleModel = curVehicle.VehicleModel;
                    curData.NumberOfTrips = _numberOfTrips;
                    curData.TotalMileage = _totalMileage;
                    curData.AverageMileage = _averageMileage;
                    curData.TotalTravelTimeDec = _totalTravelTime;
                    curData.TotalTravelTime = ConvertToTimeFormat(_totalTravelTime);
                    curData.AverageTravelTime = ConvertToTimeFormat(_averageTravelTime);
                    
                    viewModel._vehicleUsageList.Add(curData);
                    viewModel._DMCVehicleList.Add(curVehicleID + " - " + curVehicle.VehicleModel);
                    
                    if (_cycleCount == TripListByVehicleID.Count() && curVehicleID != data.EquipmentNo)
                    {
                        _numberOfTrips = 1;
                        _totalMileage += data.TripLength;
                        _totalTravelTime += data.TripDurationDec;

                        curVehicle = db.DMCVehicleLists.Where(s => s.PlateNumber == data.EquipmentNo).SingleOrDefault();
                        curData = new VehicleUsage();

                        _averageMileage = Math.Round((_totalMileage / _numberOfTrips), 2);
                        _averageTravelTime = Math.Floor((_totalTravelTime / _numberOfTrips));

                        curData.VehicleID = data.EquipmentNo;
                        curData.VehicleModel = curVehicle.VehicleModel;
                        curData.NumberOfTrips = _numberOfTrips;
                        curData.TotalMileage = _totalMileage;
                        curData.AverageMileage = _averageMileage;
                        curData.TotalTravelTimeDec = _totalTravelTime;
                        curData.TotalTravelTime = ConvertToTimeFormat(_totalTravelTime);
                        curData.AverageTravelTime = ConvertToTimeFormat(_averageTravelTime);

                        viewModel._vehicleUsageList.Add(curData);
                        viewModel._DMCVehicleList.Add(data.EquipmentNo + " - " + curVehicle.VehicleModel);
                    }

                    _numberOfTrips = 0;
                    curVehicleID = data.EquipmentNo;
                    _totalMileage = _totalTravelTime = _averageMileage = _averageTravelTime = 0;
                }

                _numberOfTrips += 1;
                _totalMileage += data.TripLength;
                _totalTravelTime += data.TripDurationDec;
            }
            
            int _counter = 0;
            foreach (var data in viewModel._vehicleUsageList)
            {
                if (data.NumberOfTrips > viewModel.UsageHighlights.VehicleNoOfTrips)
                {
                    DMCVehicleList vehicle = db.DMCVehicleLists.Where(s => s.PlateNumber == data.VehicleID).SingleOrDefault();

                    viewModel.UsageHighlights.VehicleMostTrips = vehicle.PlateNumber;
                    viewModel.UsageHighlights.VehicleType = vehicle.VehicleModel;
                    viewModel.UsageHighlights.VehicleNoOfTrips = data.NumberOfTrips;
                }

                viewModel.UsageHighlights.TotalTrips += data.NumberOfTrips;
                viewModel.UsageHighlights.TotalMileage += data.TotalMileage;
                viewModel.UsageHighlights.TotalTravelTime += data.TotalTravelTimeDec;
                _counter++;
            }

            var DMCVechiclesDB = db.DMCVehicleLists.ToList();
            foreach (var data in DMCVechiclesDB)
            {
                if (viewModel._DMCVehicleList.Contains(data.PlateNumber + " - " + data.VehicleModel) != true)
                    viewModel._DMCVehicleNotUsedList.Add(data.PlateNumber + " - " + data.VehicleModel);
            }
            viewModel.UsageHighlights.VehiclesNotUsed = viewModel._DMCVehicleNotUsedList.Count();
            viewModel.UsageHighlights.VehiclesUsed = viewModel._DMCVehicleList.Count();

            if (_counter > 0)
            {
                decimal NoOfDaysInBetween = ((TimeSpan)(dateTo - dateFrom)).Days;

                viewModel.UsageHighlights.AverageTrips = Math.Round((Convert.ToDecimal(viewModel.UsageHighlights.TotalTrips) / NoOfDaysInBetween), 1);
                viewModel.UsageHighlights.AverageMileage = Math.Floor(viewModel.UsageHighlights.TotalMileage / NoOfDaysInBetween);
                
                decimal _tempConvertedToHour = viewModel.UsageHighlights.TotalTravelTime / 60;
                _tempConvertedToHour = _tempConvertedToHour / 60;
                decimal _tempAveTravelTime = Math.Round((_tempConvertedToHour / NoOfDaysInBetween), 1);
                viewModel.UsageHighlights.TotalTravelTime = Math.Round(_tempConvertedToHour, 2);
                viewModel.UsageHighlights.AverageTravelTime = _tempAveTravelTime;
            }
        }

        public void IdentifyUsageByTimeRange()
        {
            viewModel = (ReportsViewModel)Session["mySession"];
            viewModel._usageByTimeRangeList = new List<UsageByTimeRange>()
            {
                new UsageByTimeRange() { TimeRange="00:00-01:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="01:01-02:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="02:01-03:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="03:01-04:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="04:01-05:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="05:01-06:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="06:01-07:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="07:01-08:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="08:01-09:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="09:01-10:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="10:01-11:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="11:01-12:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="12:01-13:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="13:01-14:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="14:01-15:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="15:01-16:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="16:01-17:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="17:01-18:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="18:01-19:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="19:01-20:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="20:01-21:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="21:01-22:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="22:01-23:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" },
                new UsageByTimeRange() { TimeRange="23:01-24:00", NumberOfTrips=0, TotalMileage=0, AverageMileage=0, TotalTravelTime="00:00:00", AverageTravelTime="00:00:00" }
            };
            
            foreach (var data in TripReportRawList)
            {
                string _startTimeHour = data.StartTime.Substring(0, 2);
                string _startTimeSec = data.StartTime.Substring(4, 2);
                int _index = 0;
                if (_startTimeHour == "00" || (_startTimeHour == "01" && _startTimeSec == "00"))
                    _index = 0;
                else if (_startTimeHour == "01" || (_startTimeHour == "02" && _startTimeSec == "00"))
                    _index = 1;
                else if (_startTimeHour == "02" || (_startTimeHour == "03" && _startTimeSec == "00"))
                    _index = 2;
                else if (_startTimeHour == "03" || (_startTimeHour == "04" && _startTimeSec == "00"))
                    _index = 3;
                else if (_startTimeHour == "04" || (_startTimeHour == "05" && _startTimeSec == "00"))
                    _index = 4;
                else if (_startTimeHour == "05" || (_startTimeHour == "06" && _startTimeSec == "00"))
                    _index = 5;
                else if (_startTimeHour == "06" || (_startTimeHour == "07" && _startTimeSec == "00"))
                    _index = 6;
                else if (_startTimeHour == "07" || (_startTimeHour == "08" && _startTimeSec == "00"))
                    _index = 7;
                else if (_startTimeHour == "08" || (_startTimeHour == "09" && _startTimeSec == "00"))
                    _index = 8;
                else if (_startTimeHour == "09" || (_startTimeHour == "10" && _startTimeSec == "00"))
                    _index = 9;
                else if (_startTimeHour == "10" || (_startTimeHour == "11" && _startTimeSec == "00"))
                    _index = 10;
                else if (_startTimeHour == "11" || (_startTimeHour == "12" && _startTimeSec == "00"))
                    _index = 11;
                else if (_startTimeHour == "12" || (_startTimeHour == "13" && _startTimeSec == "00"))
                    _index = 12;
                else if (_startTimeHour == "13" || (_startTimeHour == "14" && _startTimeSec == "00"))
                    _index = 13;
                else if (_startTimeHour == "14" || (_startTimeHour == "15" && _startTimeSec == "00"))
                    _index = 14;
                else if (_startTimeHour == "15" || (_startTimeHour == "16" && _startTimeSec == "00"))
                    _index = 15;
                else if (_startTimeHour == "16" || (_startTimeHour == "17" && _startTimeSec == "00"))
                    _index = 16;
                else if (_startTimeHour == "17" || (_startTimeHour == "18" && _startTimeSec == "00"))
                    _index = 17;
                else if (_startTimeHour == "18" || (_startTimeHour == "19" && _startTimeSec == "00"))
                    _index = 18;
                else if (_startTimeHour == "19" || (_startTimeHour == "20" && _startTimeSec == "00"))
                    _index = 19;
                else if (_startTimeHour == "20" || (_startTimeHour == "21" && _startTimeSec == "00"))
                    _index = 20;
                else if (_startTimeHour == "21" || (_startTimeHour == "22" && _startTimeSec == "00"))
                    _index = 21;
                else if (_startTimeHour == "22" || (_startTimeHour == "23" && _startTimeSec == "00"))
                    _index = 22;
                else if (_startTimeHour == "23" || (_startTimeHour == "24" && _startTimeSec == "00"))
                    _index = 23;
                
                bool present = viewModel._usageByTimeRangeList[_index].VehiclesList.Contains(data.EquipmentNo);
                if (!present)
                    viewModel._usageByTimeRangeList[_index].VehiclesList.Add(data.EquipmentNo);

                viewModel._usageByTimeRangeList[_index].NumberOfTrips += 1;
                viewModel._usageByTimeRangeList[_index].TotalMileage += data.TripLength;
                viewModel._usageByTimeRangeList[_index].TotalTravelTimeDec += data.TripDurationDec;
            }

            int index = 0;
            foreach (var data in viewModel._usageByTimeRangeList)
            {
                if (data.TotalMileage > 0)
                    viewModel._usageByTimeRangeList[index].AverageMileage = Math.Round((data.TotalMileage / data.NumberOfTrips), 2);
                if (data.TotalTravelTimeDec > 0)
                {
                    decimal _tempAveTravelTime = Math.Floor((data.TotalTravelTimeDec / data.NumberOfTrips));
                    viewModel._usageByTimeRangeList[index].AverageTravelTime = ConvertToTimeFormat(_tempAveTravelTime);
                    viewModel._usageByTimeRangeList[index].TotalTravelTime = ConvertToTimeFormat(data.TotalTravelTimeDec);
                }

                if (viewModel._usageByTimeRangeList[index].NumberOfTrips > viewModel.UsageHighlights.TimeRangeNoOfTrips)
                {
                    viewModel.UsageHighlights.TimeRangeMostTrips = viewModel._usageByTimeRangeList[index].TimeRange;
                    viewModel.UsageHighlights.TimeRangeNoOfTrips = viewModel._usageByTimeRangeList[index].NumberOfTrips;
                }
                if (viewModel._usageByTimeRangeList[index].VehiclesList.Count() > viewModel.UsageHighlights.TimeRangeNoOfVehicles)
                {
                    viewModel.UsageHighlights.TimeRangeMostVehicles = viewModel._usageByTimeRangeList[index].TimeRange;
                    viewModel.UsageHighlights.TimeRangeNoOfVehicles = viewModel._usageByTimeRangeList[index].VehiclesList.Count();
                }

                index++;
            }
        }
        
        public void IdentifyTrips(DateTime? dateFrom, DateTime? dateTo)
        {
            var tripReportRawDB = db.TripReportRaw_vw.Where(s => s.ReportDateFrom >= dateFrom && s.ReportDateFrom <= dateTo).OrderBy(s => s.TrackerID).ToList();

            int _StopsCount = 0;
            string _endTime = "";
            string _startTime = "";
            int _currentTrackerID = 0;
            decimal _TotalTripLength = 0;
            decimal _TotalTripDuration = 0;
            decimal? _TotalIdleDuration = 0;
            decimal? _TotalStopDuration = 0;
            string _SpottedEndLocation = "";
            string _SpottedStartLocation = "";

            foreach (var data in tripReportRawDB)
            {
                #region Checking before finalizing trip data
                if (_currentTrackerID == 0)
                    _currentTrackerID = data.TrackerID;
                else if (_currentTrackerID != data.TrackerID)
                {
                    _currentTrackerID = data.TrackerID;
                    _StopsCount = 0;
                    _SpottedEndLocation = _SpottedStartLocation = _endTime = _startTime = "";
                    _TotalTripLength = _TotalTripDuration =  0;
                    _TotalIdleDuration = _TotalStopDuration = 0;
                }
                
                string[] _startLocation = data.StartLocation.ToString().Split(new string[] { " - " }, StringSplitOptions.None);
                string[] _endLocation = data.EndLocation.ToString().Split(new string[] { " - " }, StringSplitOptions.None);
                
                if (_startLocation[1].Substring(0, 1) != "[" || _endLocation[1].Substring(0, 1) != "[")
                {
                    if (_startLocation[1].Substring(0, 1) == "[" && _endLocation[1].Substring(0, 1) != "[")
                    {
                        _SpottedEndLocation = _SpottedStartLocation = _endTime = _startTime = "";
                        _TotalTripLength = _TotalTripDuration = 0;
                        _TotalIdleDuration = _TotalStopDuration = 0;
                        _SpottedStartLocation = _startLocation[1];
                        _startTime = _startLocation[0];
                        _StopsCount = 0;
                    }
                    if (_startLocation[1].Substring(0, 1) != "[" && _endLocation[1].Substring(0, 1) == "[" && _SpottedStartLocation != "")
                    {
                        _TotalTripLength = _TotalTripLength + data.TripLength;
                        _TotalTripDuration = _TotalTripDuration + data.TravelTime;
                        _TotalIdleDuration = _TotalIdleDuration + data.IdleDuration;
                        _TotalStopDuration = _TotalStopDuration + data.StopDuration;
                        _SpottedEndLocation = _endLocation[1];
                        _endTime = _endLocation[0];
                    }
                    if (_SpottedStartLocation == "" || _SpottedEndLocation == "")
                    {
                        _StopsCount = _StopsCount + 1;
                        _TotalTripLength = _TotalTripLength + data.TripLength;
                        _TotalTripDuration = _TotalTripDuration + data.TravelTime;
                        _TotalIdleDuration = _TotalIdleDuration + data.IdleDuration;
                        _TotalStopDuration = _TotalStopDuration + data.StopDuration;
                        continue;
                    }
                }

                if (_startLocation[1].Substring(0, 1) == "[" && _endLocation[1].Substring(0, 1) == "[")
                {
                    _StopsCount = 0;
                    _endTime = _endLocation[0];
                    _startTime = _startLocation[0];
                    _TotalTripLength = data.TripLength;
                    _TotalTripDuration = data.TravelTime;
                    _TotalIdleDuration = _TotalStopDuration = 0;
                    _SpottedEndLocation = _endLocation[1];
                    _SpottedStartLocation = _startLocation[1];
                }
                #endregion

                TripsReportRaw curData = new TripsReportRaw();

                curData.ReportDate        = data.ReportDateFrom.ToString("yyyy-MM-dd");
                curData.EquipmentNo       = data.Header.ToString();
                curData.TripName          = FormatTrip(_SpottedStartLocation, _SpottedEndLocation, "name");
                curData.StartLocation     = FormatTrip(_SpottedStartLocation, _SpottedEndLocation, "start");
                curData.EndLocation       = FormatTrip(_SpottedStartLocation, _SpottedEndLocation, "end");
                curData.TripLength        = _TotalTripLength;
                curData.TripDuration      = ConvertToTimeFormat(_TotalTripDuration);
                curData.TripDurationDec   = _TotalTripDuration;
                curData.StopsInBetween    = _StopsCount.ToString();
                curData.TripIdlingTime    = ConvertToTimeFormat(Convert.ToDecimal(_TotalIdleDuration));
                curData.TripIdlingTimeDec = _TotalIdleDuration;
                curData.TripStopTime      = ConvertToTimeFormat(Convert.ToDecimal(_TotalStopDuration));
                curData.TripStopTimeDec   = _TotalStopDuration;
                curData.StartTime         = _startTime;
                curData.EndTime           = _endTime;

                _StopsCount = 0;
                _SpottedEndLocation = _SpottedStartLocation = _endTime = _startTime = "";
                _TotalTripLength = _TotalTripDuration = 0;
                TripReportRawList.Add(curData);
            }
            viewModel._tripReportRaw = TripReportRawList;
        }

        public void IdentifyTripsBySelectedTime()
        {
            viewModel = (ReportsViewModel)Session["mySession"];

            List<TripsReportRaw> tempTripReportRawList = new List<TripsReportRaw>();
            foreach (var data in TripReportRawList)
                tempTripReportRawList.Add(data);

            foreach (var data in tempTripReportRawList)
            {
                bool accepted = false;

                int _startTimeHour = 0;
                int _startTimeSec = 0;
                if (data.StartTime.Substring(0, 2) != "00")
                    _startTimeHour = Convert.ToInt32(data.StartTime.Substring(0, 2).TrimStart(new char[] { '0' }));
                if (data.StartTime.Substring(3, 2) != "00")
                    _startTimeSec = Convert.ToInt32(data.StartTime.Substring(3, 2).TrimStart(new char[] { '0' }));
                
                if (_startTimeHour >= viewModel.StartTime && _startTimeHour <= viewModel.EndTime)
                {
                    accepted = true;
                    if (_startTimeHour == viewModel.EndTime && _startTimeSec > 0)
                        accepted = false;
                }

                if (!accepted)
                    TripReportRawList.Remove(data);
            }
            
            viewModel._tripReportRaw = TripReportRawList;
        }

        public void IdentifyIdlingViolations(string site)
        {
            viewModel = (ReportsViewModel)Session["mySession"];
            var IdlingListDB = db.ExcessiveIdling_vw.Where(s => s.ReportDateFrom >= viewModel.DateFROM && s.ReportDateFrom <= viewModel.DateTO && s.Site == "DMC").OrderBy(s => s.Header).ToList();
            if (site == "RTN")
                IdlingListDB = db.ExcessiveIdling_vw.Where(s => s.ReportDateFrom >= viewModel.DateFROM && s.ReportDateFrom <= viewModel.DateTO && s.Site == "RTN").OrderBy(s => s.Header).ToList();

            foreach (var data in IdlingListDB)
            {
                bool accepted = false;
                int _startTimeHour = 0;
                int _startTimeSec = 0;
                if (data.TripTimeStart.Substring(0, 2) != "00")
                    _startTimeHour = Convert.ToInt32(data.TripTimeStart.Substring(0, 2).TrimStart(new char[] { '0' }));
                if (data.TripTimeStart.Substring(3, 2) != "00")
                    _startTimeSec = Convert.ToInt32(data.TripTimeStart.Substring(3, 2).TrimStart(new char[] { '0' }));

                if (_startTimeHour >= viewModel.StartTime && _startTimeHour <= viewModel.EndTime)
                {
                    accepted = true;
                    if (_startTimeHour == viewModel.EndTime && _startTimeSec > 0)
                        accepted = false;
                }

                if (accepted)
                {
                    DMCVehicleList models = db.DMCVehicleLists.Where(s => s.PlateNumber == data.Header).FirstOrDefault();
                    if (models != null)
                        data.VehicleModel = models.VehicleModel;
                    viewModel._excessiveIdlingList.Add(data);
                    viewModel._excessiveIdlingListCopy.Add(data);
                }
            }

            if (viewModel._excessiveIdlingList.Count() > 0)
            {
                var IdlingListByVehicle = viewModel._excessiveIdlingList.GroupBy(s => s.Header).Select(sg =>
                                                                new {
                                                                    Vehicle = sg.Key,
                                                                    IdlingCount = sg.Count()
                                                                }).OrderByDescending(s => s.IdlingCount).ToList();

                var IdlingListByLocation = viewModel._excessiveIdlingList.GroupBy(s => s.Address).Select(sg =>
                                                                new {
                                                                    Address = sg.Key,
                                                                    AddressCount = sg.Count()
                                                                }).OrderByDescending(s => s.AddressCount).ToList();

                var IdlingListByTimeRange = viewModel._excessiveIdlingList.GroupBy(s => s.TripTimeStart.Substring(0, 2)).Select(sg =>
                                                                new {
                                                                    TimeRange = sg.Key,
                                                                    TimeRangeCount = sg.Count()
                                                                }).OrderByDescending(s => s.TimeRangeCount).ToList();

                string _timeRangeTemp = IdlingListByTimeRange[0].TimeRange;
                if (_timeRangeTemp == "00")
                    _timeRangeTemp = "00:00-01:00";
                else
                {
                    string _secondHour = (Convert.ToInt32(_timeRangeTemp.TrimStart(new char[] { '0' })) + 1).ToString().PadLeft(2, '0');
                    _timeRangeTemp = _timeRangeTemp + ":01-" + _secondHour + ":00";
                }
                string[] _locationTemp = IdlingListByLocation[0].Address.Split(']');
                string _finalLocationTemp = _locationTemp[0].Replace("[", "");

                string _vehicleMostidling = IdlingListByVehicle[0].Vehicle;
                string tempVehicleModel = "";
                if (site == "DMC")
                {
                    DMCVehicleList vehicle = db.DMCVehicleLists.Where(s => s.PlateNumber == _vehicleMostidling).SingleOrDefault();
                    tempVehicleModel = vehicle.VehicleModel;
                }
                else if (site == "RTN")
                    tempVehicleModel = " ";

                viewModel.NoncompliantHighlights.TotalIdlingCount = viewModel._excessiveIdlingList.Count();
                viewModel.NoncompliantHighlights.VehicleMostIdling = IdlingListByVehicle[0].Vehicle;
                viewModel.NoncompliantHighlights.VehicleMostIdlingCount = IdlingListByVehicle[0].IdlingCount;
                viewModel.NoncompliantHighlights.VehicleMostIdlingModel = tempVehicleModel;
                viewModel.NoncompliantHighlights.LocationMostIdling = _finalLocationTemp;
                viewModel.NoncompliantHighlights.LocationMostIdlingCount = IdlingListByLocation[0].AddressCount;
                viewModel.NoncompliantHighlights.TimeRangeMostIdling = _timeRangeTemp;
                viewModel.NoncompliantHighlights.TimeRangeMostIdlingCount = IdlingListByTimeRange[0].TimeRangeCount;
            }
        }

        public void IdentifySpeedingViolations(string site)
        {
            viewModel = (ReportsViewModel)Session["mySession"];
            
            var SpeedingListDB = db.SpeedViolation_vw.Where(s => s.ReportDateFrom >= viewModel.DateFROM && s.ReportDateFrom <= viewModel.DateTO && s.Site == "DMC").OrderBy(s => s.PlateNumber).ToList();
            if (site == "RTN")
                SpeedingListDB = db.SpeedViolation_vw.Where(s => s.ReportDateFrom >= viewModel.DateFROM && s.ReportDateFrom <= viewModel.DateTO && s.Site == "RTN").OrderBy(s => s.PlateNumber).ToList();
            
            foreach (var data in SpeedingListDB)
            {
                System.Diagnostics.Debug.WriteLine("Current Speeding Data: " + data.PlateNumber + " | Report Date From: " + data.ReportDateFrom + " | Start Time: " + data.StartTime);
                bool accepted = false;
                int _startTimeHour = 0;
                int _startTimeSec = 0;
                if (data.StartTime.Substring(0, 2) != "00")
                    _startTimeHour = Convert.ToInt32(data.StartTime.Substring(0, 2).TrimStart(new char[] { '0' }));
                if (data.StartTime.Substring(3, 2) != "00")
                    _startTimeSec = Convert.ToInt32(data.StartTime.Substring(3, 2).TrimStart(new char[] { '0' }));

                if (_startTimeHour >= viewModel.StartTime && _startTimeHour <= viewModel.EndTime)
                {
                    accepted = true;
                    if (_startTimeHour == viewModel.EndTime && _startTimeSec > 0)
                        accepted = false;
                }

                if (accepted)
                {
                    viewModel._overSpeedingList.Add(data);
                    viewModel._overSpeedingListCopy.Add(data);
                }
            }

            if (viewModel._overSpeedingList.Count() > 0)
            {
                var SpeedingListByVehicle = viewModel._overSpeedingList.GroupBy(s => s.EquipmentID).Select(sg =>
                                                                new {
                                                                    Vehicle = sg.Key,
                                                                    SpeedingCount = sg.Count()
                                                                }).OrderByDescending(s => s.SpeedingCount).ToList();

                var SpeedingListByLocation = viewModel._overSpeedingList.GroupBy(s => s.Address).Select(sg =>
                                                                new {
                                                                    Address = sg.Key,
                                                                    AddressCount = sg.Count()
                                                                }).OrderByDescending(s => s.AddressCount).ToList();

                var SpeedingListByTimeRange = viewModel._overSpeedingList.GroupBy(s => s.StartTime.Substring(0, 2)).Select(sg =>
                                                                new {
                                                                    TimeRange = sg.Key,
                                                                    TimeRangeCount = sg.Count()
                                                                }).OrderByDescending(s => s.TimeRangeCount).ToList();

                string _timeRangeTemp = SpeedingListByTimeRange[0].TimeRange;
                if (_timeRangeTemp == "00")
                    _timeRangeTemp = "00:00-01:00";
                else
                {
                    string _secondHour = (Convert.ToInt32(_timeRangeTemp.TrimStart(new char[] { '0' })) + 1).ToString().PadLeft(2, '0');
                    _timeRangeTemp = _timeRangeTemp + ":01-" + _secondHour + ":00";
                }
                string[] _locationTemp = SpeedingListByLocation[0].Address.Split(']');
                string _finalLocationTemp = _locationTemp[0].Replace("[", "");

                string _vehicleMostSpeeding = SpeedingListByVehicle[0].Vehicle;
                string tempVehicleModel = "";
                if (site == "DMC")
                {
                    DMCVehicleList vehicle = db.DMCVehicleLists.Where(s => s.PlateNumber == _vehicleMostSpeeding).SingleOrDefault();
                    tempVehicleModel = vehicle.VehicleModel;
                }
                else if (site == "RTN")
                    tempVehicleModel = " ";

                viewModel.NoncompliantHighlights.TotalSpeedingCount = viewModel._overSpeedingList.Count();
                viewModel.NoncompliantHighlights.VehicleMostSpeeding = SpeedingListByVehicle[0].Vehicle;
                viewModel.NoncompliantHighlights.VehicleMostSpeedingCount = SpeedingListByVehicle[0].SpeedingCount;
                viewModel.NoncompliantHighlights.VehicleMostSpeedingModel = tempVehicleModel;
                viewModel.NoncompliantHighlights.LocationMostSpeeding = _finalLocationTemp;
                viewModel.NoncompliantHighlights.LocationMostSpeedingCount = SpeedingListByLocation[0].AddressCount;
                viewModel.NoncompliantHighlights.TimeRangeMostSpeeding = _timeRangeTemp;
                viewModel.NoncompliantHighlights.TimeRangeMostSpeedingCount = SpeedingListByTimeRange[0].TimeRangeCount;
            }
        }

        private string ConvertToTimeFormat(decimal _input)
        {
            string _seconds = Math.Truncate(_input % 60).ToString();
            if (_seconds == "0") _seconds = "00";
            string _minutes = Math.Truncate(_input / 60).ToString();
            string _hours = "00";
            if (Convert.ToInt64(_minutes) > 59)
            {
                _hours = Math.Truncate(Convert.ToDecimal(_minutes) / 60).ToString();
                _minutes = (Convert.ToInt64(_minutes) % 60).ToString();
            }

            string _finalString = _hours.PadLeft(2, '0') + ":" + _minutes.PadLeft(2, '0') + ":" + _seconds.PadLeft(2, '0');
            return _finalString;
        }
        private string ConvertToHrMinFormat(decimal _input)
        {
            string _finalString = "";
            string _convertedToTimeFormat = ConvertToTimeFormat(_input);
            if (_convertedToTimeFormat.Substring(0, 2) == "00")
                _finalString = _convertedToTimeFormat.Substring(3, 2).TrimStart('0') + " min(s)";
            else
                _finalString = _convertedToTimeFormat.Substring(0, 2).TrimStart(new char[] { '0' }) + " hr(s) " + _convertedToTimeFormat.Substring(3, 2).TrimStart(new char[] { '0' }) + "min(s)";

            return _finalString;
        }
        private string FormatTrip(string _startLocation, string _endLocation, string choice)
        {
            string _finalTripName = "";
            string _finalStartLoc = "";
            string _finalEndLoc = "";
            string _startGeofence = "";
            string _endGeofence = "";

            string[] _startContent = _startLocation.Split(new string[] { "] " }, StringSplitOptions.None);
            string[] _endContent = _endLocation.Split(new string[] { "] " }, StringSplitOptions.None);

            for (int i = 0; i < _startContent.Count(); i++)
            {
                if (_startContent[i].Substring(0, 1) == "[")
                    _startGeofence = _startGeofence + _startContent[i] + "] ";
            }
            for (int i = 0; i < _endContent.Count(); i++)
            {
                if (_endContent[i].Substring(0, 1) == "[")
                    _endGeofence = _endGeofence + _endContent[i] + "] ";
            }

            _finalTripName = _startGeofence + "- " + _endGeofence;
            _finalStartLoc = _startContent[(_startContent.Count() - 1)];
            _finalEndLoc = _endContent[(_endContent.Count() - 1)];

            if (choice == "start")
                return _startGeofence;
            else if (choice == "end")
                return _endGeofence;

            return _finalTripName;
        }

        // GET: Reports
        public void CreateExcel()
        {
            ReportExcel excel = new ReportExcel();
            viewModel = (ReportsViewModel)Session["mySession"];

            Response.ClearContent();
            Response.BinaryWrite(excel.GenerateTripReportRawExcel());
            string _filename = "GPS REPORT " + DateTime.Now.ToString("yyyyMMdd") + "_" + DateTime.Now.ToString("HHmm") + ".xlsx";
            Response.AddHeader("content-disposition", "attachment: filename=" + _filename);
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            if (Response.IsClientConnected)
            {
                Response.Flush();
                Response.End();
            }

            System.Diagnostics.Debug.WriteLine("Generating Excel");
        }
    }

    public class ReportExcel
    {
        int rowIndex = 2;
        ExcelRange cell;
        ExcelFill fill;
        Border border;

        private void AddCell(ExcelWorksheet _sheet, int _fromCol, int _toRow, int _toCol, string _cellValue,
                                bool _bold, int _fontSize, string _hAlign, string _vAlign, string _borderStyle, Color _bgColor)
        {
            System.Diagnostics.Debug.WriteLine("Current Cell : " + _cellValue + " | Cell coordinates : " + rowIndex + ", " + _fromCol + ", " + _toRow + ", " + _toCol);
            if (_toRow > 0 && _toCol > 0)
            {
                _sheet.Cells[rowIndex, _fromCol, _toRow, _toCol].Merge = true;
                cell = _sheet.Cells[rowIndex, _fromCol, _toRow, _toCol];
            }
            else
                cell = _sheet.Cells[rowIndex, _fromCol];

            cell.Value = _cellValue;
            cell.Style.Font.Size = _fontSize;
            cell.Style.WrapText = true;

            if (_bold == true)
                cell.Style.Font.Bold = true;

            if (_hAlign == "right")
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            else if (_hAlign == "left")
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            else if (_hAlign == "center")
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            if (_vAlign == "top")
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            else if (_vAlign == "bottom")
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            else if (_vAlign == "center")
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            
            fill = cell.Style.Fill;
            fill.PatternType = ExcelFillStyle.Solid;
            fill.BackgroundColor.SetColor(_bgColor);
            border = cell.Style.Border;

            if (_borderStyle == "thin")
                border.Bottom.Style = border.Top.Style = border.Left.Style = border.Right.Style = ExcelBorderStyle.Thin;
            else if (_borderStyle == "thick")
                border.Bottom.Style = border.Top.Style = border.Left.Style = border.Right.Style = ExcelBorderStyle.Thick;
        }

        public byte[] GenerateTripReportRawExcel()
        {
            using (var excelPackage = new ExcelPackage())
            {
                excelPackage.Workbook.Properties.Author = "NAC PD&R";
                excelPackage.Workbook.Properties.Title = "GPS REPORTING";
                ReportsViewModel viewModel = (ReportsViewModel)System.Web.HttpContext.Current.Session["mySession"];

                //by vehicle usage
                #region Set Column Width
                var sheet = excelPackage.Workbook.Worksheets.Add("By vehicle usage");
                sheet.Name = "By vehicle usage";
                sheet.Column(1).Width = 3;   //Empty space
                sheet.Column(2).Width = 20;  //Vehicle ID
                sheet.Column(3).Width = 20;  //Number of Trips
                sheet.Column(4).Width = 30;  //Mileage, Total
                sheet.Column(5).Width = 30;  //Mileage, Average
                sheet.Column(6).Width = 30;  //Travel Time, Total
                sheet.Column(7).Width = 30;  //Travel Time, Average
                #endregion
                #region Set Table Header
                rowIndex = 2;

                string _text = "TRIP REPORT BREAKDOWN BY VEHICLE USAGE";
                AddCell(sheet, 2, 2, 7, _text, true, 15, "center", "center", "none", Color.White);
                rowIndex++;
                _text = "(Start date: " + viewModel.DateFROM.Value.ToString("yyyy-MM-dd") + " End Date: " + viewModel.DateTO.Value.ToString("yyyy-MM-dd") + ")";
                AddCell(sheet, 2, 3, 7, _text, false, 15, "center", "center", "thin", Color.LightGreen);
                rowIndex += 2;
                
                AddCell(sheet, 2, 6, 2, "Vehicle ID", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 3, 6, 3, "Number of Trips.", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 4, 5, 5, "Mileage, km", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 6, 5, 7, "Travel Time, hh:mm:ss", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                rowIndex++;

                AddCell(sheet, 4, 0, 0, "Total", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 5, 0, 0, "Average", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 6, 0, 0, "Total", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 7, 0, 0, "Average", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                rowIndex++;
                #endregion
                #region Set Table Body
                foreach (var data in viewModel._vehicleUsageList)
                {
                    AddCell(sheet, 2, 0, 0, data.VehicleID, false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 3, 0, 0, data.NumberOfTrips.ToString(), false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 4, 0, 0, data.TotalMileage.ToString(), false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 5, 0, 0, data.AverageMileage.ToString(), false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 6, 0, 0, data.TotalTravelTime.ToString(), false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 7, 0, 0, data.AverageTravelTime.ToString(), false, 12, "center", "center", "thin", Color.White);
                    rowIndex++;
                }
                #endregion

                //by time range
                #region Set Column Width
                sheet = excelPackage.Workbook.Worksheets.Add("By time range");
                sheet.Name = "By time range";
                sheet.Column(1).Width = 3;   //Empty space
                sheet.Column(2).Width = 20;  //Time range
                sheet.Column(3).Width = 20;  //Number of Trips
                sheet.Column(4).Width = 30;  //Mileage, Total
                sheet.Column(5).Width = 30;  //Mileage, Average
                sheet.Column(6).Width = 30;  //Travel Time, Total
                sheet.Column(7).Width = 30;  //Travel Time, Average
                #endregion
                #region Set Table Header
                rowIndex = 2;

                _text = "TRIP REPORT BREAKDOWN BY TIME RANGE";
                AddCell(sheet, 2, 2, 7, _text, true, 15, "center", "center", "none", Color.White);
                rowIndex++;
                _text = "(Start date: " + viewModel.DateFROM.Value.ToString("yyyy-MM-dd") + " End Date: " + viewModel.DateTO.Value.ToString("yyyy-MM-dd") + ")";
                AddCell(sheet, 2, 3, 7, _text, false, 15, "center", "center", "thin", Color.LightGreen);
                rowIndex += 2;

                AddCell(sheet, 2, 6, 2, "Time Range", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 3, 6, 3, "Number of Trips.", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 4, 5, 5, "Mileage, km", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 6, 5, 7, "Travel Time, hh:mm:ss", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                rowIndex++;

                AddCell(sheet, 4, 0, 0, "Total", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 5, 0, 0, "Average", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 6, 0, 0, "Total", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 7, 0, 0, "Average", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                rowIndex++;
                #endregion
                #region Set Table Body
                foreach (var data in viewModel._usageByTimeRangeList)
                {
                    AddCell(sheet, 2, 0, 0, data.TimeRange, false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 3, 0, 0, data.NumberOfTrips.ToString(), false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 4, 0, 0, data.TotalMileage.ToString(), false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 5, 0, 0, data.AverageMileage.ToString(), false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 6, 0, 0, data.TotalTravelTime.ToString(), false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 7, 0, 0, data.AverageTravelTime.ToString(), false, 12, "center", "center", "thin", Color.White);
                    rowIndex++;
                }
                #endregion

                //vehicles used and not used
                #region Set Column Width
                sheet = excelPackage.Workbook.Worksheets.Add("Vehicles used and not used");
                sheet.Name = "Vehicles used and not used";
                sheet.Column(1).Width = 3;   //Empty space
                sheet.Column(2).Width = 40;  //Vehicles used
                sheet.Column(3).Width = 40;  //Vehicles not used
                #endregion
                #region Set Table Header
                rowIndex = 2;

                _text = "VEHICLES USED AND NOT USED";
                AddCell(sheet, 2, rowIndex, 3, _text, true, 15, "center", "center", "none", Color.White);
                rowIndex++;
                _text = "(Start date: " + viewModel.DateFROM.Value.ToString("yyyy-MM-dd") + " End Date: " + viewModel.DateTO.Value.ToString("yyyy-MM-dd") + ")";
                AddCell(sheet, 2, rowIndex, 3, _text, false, 15, "center", "center", "thin", Color.LightGreen);
                rowIndex += 2;

                AddCell(sheet, 2, 0, 0, "Vehicles used", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 3, 0, 0, "Vehicles not used", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                rowIndex++;
                #endregion
                #region Set Table Body
                int temp = rowIndex;
                foreach (var data in viewModel._DMCVehicleList)
                {
                    AddCell(sheet, 2, 0, 0, data, false, 12, "center", "center", "thin", Color.White);
                    rowIndex++;
                }
                rowIndex = temp;
                foreach (var data in viewModel._DMCVehicleNotUsedList)
                {
                    AddCell(sheet, 3, 0, 0, data, false, 12, "center", "center", "thin", Color.White);
                    rowIndex++;
                }
                #endregion
                
                //by start and end location
                #region Set Column Width
                sheet = excelPackage.Workbook.Worksheets.Add("By start and end location");
                sheet.Name = "By start and end location";
                sheet.Column(1).Width = 5;   //Empty space
                sheet.Column(2).Width = 50;  //Start location
                sheet.Column(3).Width = 50;  //End location
                sheet.Column(4).Width = 15;  //Vehicle ID
                sheet.Column(5).Width = 15;  //Number of trips
                sheet.Column(6).Width = 20;  //Average travel time
                sheet.Column(7).Width = 20;  //Average idling time
                sheet.Column(8).Width = 20;  //Average stop time
                #endregion
                #region Set Table Header
                rowIndex = 2;

                _text = "TRIP REPORT BREAKDOWN BY START AND END LOCATION";
                AddCell(sheet, 2, 2, 8, _text, true, 15, "center", "center", "none", Color.White);
                rowIndex++;
                _text = "(Start date: " + viewModel.DateFROM.Value.ToString("yyyy-MM-dd") + " End Date: " + viewModel.DateTO.Value.ToString("yyyy-MM-dd") + ")";
                AddCell(sheet, 2, 3, 8, _text, false, 15, "center", "center", "thin", Color.LightGreen);
                rowIndex += 2;

                AddCell(sheet, 2, 0, 0, "Start location", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 3, 0, 0, "End location", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 4, 0, 0, "Vehicle ID", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 5, 0, 0, "Number of trips", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 6, 0, 0, "Average travel time", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 7, 0, 0, "Average idling time", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 8, 0, 0, "Average stop time", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                rowIndex++;
                #endregion
                #region Set Table Body
                foreach (var data in viewModel._keyTripsList)
                {
                    AddCell(sheet, 2, 0, 0, data.StartLocation, false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 3, 0, 0, data.EndLocation, false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 4, 0, 0, data.VehicleID, false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 5, 0, 0, data.NumberOfTrips.ToString(), false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 6, 0, 0, data.AverageTravelTime, false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 7, 0, 0, data.AverageIdlingTime.ToString(), false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 8, 0, 0, data.AverageStopTime.ToString(), false, 12, "center", "center", "thin", Color.White);
                    rowIndex++;
                }
                #endregion

                //Excessive idling
                #region Set Column Width
                sheet = excelPackage.Workbook.Worksheets.Add("Excessive idling");
                sheet.Name = "Excessive idling";
                sheet.Column(1).Width = 5;   //Empty space
                sheet.Column(2).Width = 15;  //Plate Number
                sheet.Column(3).Width = 25;  //Vehicle Model
                sheet.Column(4).Width = 15;  //Date
                sheet.Column(5).Width = 15;  //Time Start
                sheet.Column(6).Width = 15;  //Time End
                sheet.Column(7).Width = 70;  //Location
                sheet.Column(8).Width = 20;  //Parking Duration
                sheet.Column(9).Width = 20;  //Idling Duration
                #endregion
                #region Set Table Header
                rowIndex = 2;

                _text = "EXCESSIVE IDLING";
                AddCell(sheet, 2, 2, 9, _text, true, 15, "center", "center", "none", Color.White);
                rowIndex++;
                _text = "(Start date: " + viewModel.DateFROM.Value.ToString("yyyy-MM-dd") + " End Date: " + viewModel.DateTO.Value.ToString("yyyy-MM-dd") + ")";
                AddCell(sheet, 2, 3, 9, _text, false, 15, "center", "center", "thin", Color.LightGreen);
                rowIndex += 2;

                AddCell(sheet, 2, 0, 0, "Plate Number", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 3, 0, 0, "Vehicle Model", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 4, 0, 0, "Date", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 5, 0, 0, "Parking Start, hh:mm:ss", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 6, 0, 0, "Parking End, hh:mm:ss", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 7, 0, 0, "Location", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 8, 0, 0, "Parking duration, hh:mm:ss", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 9, 0, 0, "Idling duration (Ignition on while parked), hh:mm:ss", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                rowIndex++;
                #endregion
                #region Set Table Body
                foreach (var data in viewModel._excessiveIdlingList)
                {
                    AddCell(sheet, 2, 0, 0, data.Header, false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 3, 0, 0, data.VehicleModel, false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 4, 0, 0, data.ReportDateFrom.ToString("MM-dd-yyyy"), false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 5, 0, 0, data.TripTimeStart, false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 6, 0, 0, data.TripTimeEnd, false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 7, 0, 0, data.Address, false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 8, 0, 0, data.StopDuration, false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 9, 0, 0, data.IdlingDuration.ToString(), false, 12, "center", "center", "thin", Color.White);
                    rowIndex++;
                }
                #endregion

                //Speed violations
                #region Set Column Width
                sheet = excelPackage.Workbook.Worksheets.Add("Speed violations");
                sheet.Name = "Speed violations";
                sheet.Column(1).Width = 5;   //Empty space
                sheet.Column(2).Width = 15;  //Vehicle
                sheet.Column(3).Width = 15;  //Date
                sheet.Column(4).Width = 20;  //Time Start
                sheet.Column(5).Width = 70;  //Address
                sheet.Column(6).Width = 20;  //Average Speed
                sheet.Column(7).Width = 20;  //Max Speed
                #endregion
                #region Set Table Header
                rowIndex = 2;

                _text = "SPEED VIOLATIONS";
                AddCell(sheet, 2, 2, 7, _text, true, 15, "center", "center", "none", Color.White);
                rowIndex++;
                _text = "(Start date: " + viewModel.DateFROM.Value.ToString("yyyy-MM-dd") + " End Date: " + viewModel.DateTO.Value.ToString("yyyy-MM-dd") + ")";
                AddCell(sheet, 2, 3, 7, _text, false, 15, "center", "center", "thin", Color.LightGreen);
                rowIndex += 2;

                AddCell(sheet, 2, 0, 0, "Vehicle", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 3, 0, 0, "Date", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 4, 0, 0, "Time start, hh:mm:ss", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 5, 0, 0, "Location", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 6, 0, 0, "Average speed, kph", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                AddCell(sheet, 7, 0, 0, "Max speed, kph", true, 12, "center", "center", "thin", Color.LightSeaGreen);
                rowIndex++;
                #endregion
                #region Set Table Body
                foreach (var data in viewModel._overSpeedingList)
                {
                    AddCell(sheet, 2, 0, 0, data.PlateNumber, false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 3, 0, 0, data.ReportDateFrom.ToString("MM-dd-yyyy"), false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 4, 0, 0, data.StartTime, false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 5, 0, 0, data.Address, false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 6, 0, 0, data.AverageSpeed.ToString(), false, 12, "center", "center", "thin", Color.White);
                    AddCell(sheet, 7, 0, 0, data.MaxSpeed.ToString(), false, 12, "center", "center", "thin", Color.White);
                    rowIndex++;
                }
                #endregion

                return excelPackage.GetAsByteArray();
            }
        }
    }
}