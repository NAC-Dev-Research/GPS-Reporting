using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using GPSReporting.Models;
using GPSReporting.Models.RTNReports;
using System.Drawing;
using GPSReporting.DAL;
using System.Threading.Tasks;
using System.Threading;
using PagedList;

namespace GPSReporting.Controllers
{
    public class ReportsController : ReportsFunctionController
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

                if (site.ToUpper() == "DMC")
                {
                    viewModel._tripReportRaw.Clear();
                    Task SortByLocation = Task.Factory.StartNew(() => TripReportRawList = IdentifyTrips(dateFrom, dateTo, TripReportRawList));
                    Task.WaitAll(new[] { SortByLocation });
                }
                //else if (site.ToUpper() == "RTN")
                //{
                //    TripReportRawList.Clear();
                //    Task SortByLocation = Task.Factory.StartNew(() => TripReportRawList = IdentifyTripsRTN(dateFrom, dateTo, TripReportRawList));
                //    Task.WaitAll(new[] { SortByLocation });
                //    Session["RTNTripReportList"] = TripReportRawList;
                //}

                viewModel.NoncompliantHighlights = new NonCompliantHighlights();
                viewModel._excessiveIdlingList.Clear();
                viewModel._excessiveIdlingListCopy.Clear();
                viewModel._overSpeedingList.Clear();
                viewModel._overSpeedingListCopy.Clear();
                viewModel._vehicleUsageList.Clear();
                viewModel._usageByTimeRangeList.Clear();
                viewModel._keyTripsList.Clear();
                viewModel._keyTripsListCopy.Clear();
                viewModel._DMCVehicleList.Clear();
                viewModel._DMCVehicleNotUsedList.Clear();
                viewModel.UsageHighlights = new VehicleUsageHighlights();
                viewModel.KeyTripsHighlights = new KeyTripsHighlights();
                
                List<ExcessiveIdling_vw> IdlingListDB = db.ExcessiveIdling_vw.Where(s => s.ReportDateFrom >= viewModel.DateFROM && s.ReportDateFrom <= viewModel.DateTO && s.Site == "DMC").OrderBy(s => s.Header).ToList();
                if (site == "RTN")
                    IdlingListDB = db.ExcessiveIdling_vw.Where(s => s.ReportDateFrom >= viewModel.DateFROM && s.ReportDateFrom <= viewModel.DateTO && s.Site == "RTN").OrderBy(s => s.Header).ToList();
                
                List<SpeedViolation_vw> SpeedingListDB = db.SpeedViolation_vw.Where(s => s.ReportDateFrom >= viewModel.DateFROM && s.ReportDateFrom <= viewModel.DateTO && s.Site == "DMC").OrderBy(s => s.PlateNumber).ToList();
                if (site == "RTN")
                    SpeedingListDB = db.SpeedViolation_vw.Where(s => s.ReportDateFrom >= viewModel.DateFROM && s.ReportDateFrom <= viewModel.DateTO && s.Site == "RTN").OrderBy(s => s.PlateNumber).ToList();

                Task SortByIdlingViolations = Task.Factory.StartNew(() => IdentifyIdlingViolations(site, IdlingListDB));
                Task SortBySpeedingViolations = Task.Factory.StartNew(() => IdentifySpeedingViolations(site, SpeedingListDB));
                Task.WaitAll(new[] { SortByIdlingViolations, SortBySpeedingViolations });

                if (site.ToUpper() == "DMC")
                {
                    Task SortBySelectedTime = Task.Factory.StartNew(() => IdentifyTripsBySelectedTime(TripReportRawList));
                    Task.WaitAll(new[] { SortBySelectedTime });
                    
                    Task SortByVehicleNo = Task.Factory.StartNew(() => IdentifyVehicleUsage(dateFrom, dateTo, TripReportRawList));
                    Task SortByTimeRange = Task.Factory.StartNew(() => IdentifyUsageByTimeRange(TripReportRawList));
                    Task.WaitAll(new[] { SortByVehicleNo, SortByTimeRange });

                    Task SortByKeyTrips = Task.Factory.StartNew(() => IdentifyKeyTrips());
                    Task.WaitAll(new[] { SortByKeyTrips });
                }
                int NoOfDaysInBetween = ((TimeSpan)(dateTo - dateFrom)).Days;
                ViewBag.ShowReport = "USAGE";
                viewModel.DaysInBetween = NoOfDaysInBetween;
                if (viewModel.CurrentWindow == null)
                    viewModel.CurrentWindow = "USAGE";
                else
                    ViewBag.ShowReport = viewModel.CurrentWindow;

                if (viewModel._tripReportRaw.Count() <= 0 && viewModel._overSpeedingList.Count() <= 0 && viewModel._excessiveIdlingList.Count <= 0)
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