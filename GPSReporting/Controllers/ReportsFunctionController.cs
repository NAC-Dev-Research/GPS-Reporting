using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GPSReporting.DAL;
using GPSReporting.Models;
using GPSReporting.Models.RTNReports;

namespace GPSReporting.Controllers
{
    public class ReportsFunctionController : Controller
    {
        MGPSAPIEntities db = new MGPSAPIEntities();
        ReportsViewModel viewModel = new ReportsViewModel();

        public List<TripsReportRaw> IdentifyTrips(DateTime? dateFrom, DateTime? dateTo, List<TripsReportRaw> TripReportRawList)
        {
            viewModel = (ReportsViewModel)Session["mySession"];
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
                    _TotalTripLength = _TotalTripDuration = 0;
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

                curData.ReportDate = data.ReportDateFrom.ToString("yyyy-MM-dd");
                curData.EquipmentNo = data.Header.ToString();
                curData.TripName = FormatTrip(_SpottedStartLocation, _SpottedEndLocation, "name");
                curData.StartLocation = FormatTrip(_SpottedStartLocation, _SpottedEndLocation, "start");
                curData.EndLocation = FormatTrip(_SpottedStartLocation, _SpottedEndLocation, "end");
                curData.TripLength = _TotalTripLength;
                curData.TripDuration = ConvertToTimeFormat(_TotalTripDuration);
                curData.TripDurationDec = _TotalTripDuration;
                curData.StopsInBetween = _StopsCount.ToString();
                curData.TripIdlingTime = ConvertToTimeFormat(Convert.ToDecimal(_TotalIdleDuration));
                curData.TripIdlingTimeDec = _TotalIdleDuration;
                curData.TripStopTime = ConvertToTimeFormat(Convert.ToDecimal(_TotalStopDuration));
                curData.TripStopTimeDec = _TotalStopDuration;
                curData.StartTime = _startTime;
                curData.EndTime = _endTime;

                _StopsCount = 0;
                _SpottedEndLocation = _SpottedStartLocation = _endTime = _startTime = "";
                _TotalTripLength = _TotalTripDuration = 0;
                TripReportRawList.Add(curData);
            }
            viewModel._tripReportRaw = TripReportRawList;
            return TripReportRawList;
        }

        public List<TripsReportRaw> IdentifyTripsRTN(DateTime? dateFrom, DateTime? dateTo, List<TripsReportRaw> TripReportRawList)
        {
            List<vw_RTNTripReportRaw> tripReportRawDB = db.vw_RTNTripReportRaw.Where(s => s.ReportDateFrom >= dateFrom && s.ReportDateFrom <= dateTo).OrderBy(s => s.TrackerID).ThenBy(s => s.ReportDateFrom).ToList();

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
                    _TotalTripLength = _TotalTripDuration = 0;
                    _TotalIdleDuration = _TotalStopDuration = 0;
                }

                string[] _startLocation = data.MovementStart.ToString().Split(new string[] { " - " }, StringSplitOptions.None);
                string[] _endLocation = data.MovementEnd.ToString().Split(new string[] { " - " }, StringSplitOptions.None);

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

                curData.ReportDate = data.ReportDateFrom.ToString("yyyy-MM-dd");
                curData.EquipmentNo = data.EquipmentID.ToString();
                curData.EquipmentType = data.EquipmentType;
                curData.TripName = FormatTrip(_SpottedStartLocation, _SpottedEndLocation, "name");
                curData.StartLocation = FormatTrip(_SpottedStartLocation, _SpottedEndLocation, "start");
                curData.EndLocation = FormatTrip(_SpottedStartLocation, _SpottedEndLocation, "end");
                curData.TripLength = _TotalTripLength;
                curData.TripDuration = ConvertToTimeFormat(_TotalTripDuration);
                curData.TripDurationDec = _TotalTripDuration;
                curData.StopsInBetween = _StopsCount.ToString();
                curData.TripIdlingTime = ConvertToTimeFormat(Convert.ToDecimal(_TotalIdleDuration));
                curData.TripIdlingTimeDec = _TotalIdleDuration;
                curData.TripStopTime = ConvertToTimeFormat(Convert.ToDecimal(_TotalStopDuration));
                curData.TripStopTimeDec = _TotalStopDuration;
                curData.StartTime = _startTime;
                curData.EndTime = _endTime;

                _StopsCount = 0;
                _SpottedEndLocation = _SpottedStartLocation = _endTime = _startTime = "";
                _TotalTripLength = _TotalTripDuration = 0;
                TripReportRawList.Add(curData);
            }
            viewModel._tripReportRaw = TripReportRawList;
            return TripReportRawList;
        }

        public void IdentifyIdlingViolations(string site, List<ExcessiveIdling_vw> IdlingListDB)
        {
            viewModel = (ReportsViewModel)Session["mySession"];
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
        
        public void IdentifySpeedingViolations(string site, List<SpeedViolation_vw> SpeedingListDB)
        {
            viewModel = (ReportsViewModel)Session["mySession"];

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

        public void IdentifyTripsBySelectedTime(List<TripsReportRaw> TripReportRawList)
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

        public void IdentifyVehicleUsage(DateTime? dateFrom, DateTime? dateTo, List<TripsReportRaw> TripReportRawList)
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

        public void IdentifyUsageByTimeRange(List<TripsReportRaw> TripReportRawList)
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
            }
        }

        public string ConvertToTimeFormat(decimal _input)
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
        public string ConvertToHrMinFormat(decimal _input)
        {
            string _finalString = "";
            string _convertedToTimeFormat = ConvertToTimeFormat(_input);
            if (_convertedToTimeFormat.Substring(0, 2) == "00")
                _finalString = _convertedToTimeFormat.Substring(3, 2).TrimStart('0') + " min(s)";
            else
                _finalString = _convertedToTimeFormat.Substring(0, 2).TrimStart(new char[] { '0' }) + " hr(s) " + _convertedToTimeFormat.Substring(3, 2).TrimStart(new char[] { '0' }) + "min(s)";

            return _finalString;
        }
        public string FormatTrip(string _startLocation, string _endLocation, string choice)
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


    }
}