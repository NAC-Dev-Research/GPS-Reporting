using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GPSReporting.DAL;
using GPSReporting.Models;
using GPSReporting.Models.RTNReports;

namespace GPSReporting.Controllers
{
    public class RTNReportsFunctionController : Controller
    {
        MGPSAPIEntities db = new MGPSAPIEntities();

        public void CheckDateTime(ref DateTime? DateFrom, ref DateTime? DateTo, ref int? StartTime, ref int? EndTime)
        {
            if (DateFrom > DateTo)
            {
                DateTime? _tempDate = DateFrom;
                DateFrom = DateTo;
                DateTo = _tempDate;
            }

            if (StartTime == null)
                StartTime = 0;
            if (EndTime == null)
                EndTime = 24;
            if (StartTime > EndTime)
            {
                int? tempTime = StartTime;
                StartTime = EndTime;
                EndTime = tempTime;
            }
        }

        public void IdentifyTrips(DateTime? dateFrom, DateTime? dateTo, int? startTime, int? endTime, ref List<TripsReportRaw> TripReportRawList)
        {
            List<vw_RTNTripReportRaw> tripReportRawDB = db.vw_RTNTripReportRaw.Where(s => s.ReportDateFrom >= dateFrom && s.ReportDateFrom <= dateTo).OrderBy(s => s.TrackerID).ThenBy(s => s.ReportDateFrom).ThenBy(s => s.MovementStart).ToList();

            #region Initialize values
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
            #endregion

            foreach (var data in tripReportRawDB)
            {
                #region Checking before finalizing trip data
                if (_currentTrackerID == 0)
                    _currentTrackerID = data.TrackerID;
                else if (_currentTrackerID != data.TrackerID)
                {
                    _currentTrackerID = data.TrackerID;
                    ResetTripValues(ref _StopsCount, ref _SpottedStartLocation, ref _SpottedEndLocation, ref _startTime, ref _endTime, ref _TotalTripLength, ref _TotalTripDuration, ref _TotalIdleDuration, ref _TotalStopDuration);
                }

                string[] _startLocation = new string[2] { "", "" };
                string[] _endLocation = new string[2] { "", "" };
                _startLocation = data.MovementStart.ToString().Split(new string[] { " - " }, StringSplitOptions.None);
                _endLocation = data.MovementEnd.ToString().Split(new string[] { " - " }, StringSplitOptions.None);

                //checks geofence to geofence trips
                if (_startLocation[1].Substring(0, 1) != "[" || _endLocation[1].Substring(0, 1) != "[")
                {
                    if (_startLocation[1].Substring(0, 1) == "[" && _endLocation[1].Substring(0, 1) != "[")
                    {
                        ResetTripValues(ref _StopsCount, ref _SpottedStartLocation, ref _SpottedEndLocation, ref _startTime, ref _endTime, ref _TotalTripLength, ref _TotalTripDuration, ref _TotalIdleDuration, ref _TotalStopDuration);
                        _SpottedStartLocation = _startLocation[1];
                        _startTime = _startLocation[0];
                    }
                    if (_startLocation[1].Substring(0, 1) != "[" && _endLocation[1].Substring(0, 1) == "[" && _SpottedStartLocation != "")
                    {
                        _TotalTripLength = _TotalTripLength + (decimal)data.TripLength;
                        _TotalTripDuration = _TotalTripDuration + (decimal)data.TravelTime;
                        _TotalIdleDuration = _TotalIdleDuration + data.IdleDuration;
                        _TotalStopDuration = _TotalStopDuration + data.StopDuration;
                        _SpottedEndLocation = _endLocation[1];
                        _endTime = _endLocation[0];
                    }
                    if (_SpottedStartLocation == "" || _SpottedEndLocation == "")
                    {
                        _StopsCount = _StopsCount + 1;
                        _TotalTripLength = _TotalTripLength + (decimal)data.TripLength;
                        _TotalTripDuration = _TotalTripDuration + (decimal)data.TravelTime;
                        _TotalIdleDuration = _TotalIdleDuration + data.IdleDuration;
                        _TotalStopDuration = _TotalStopDuration + data.StopDuration;
                        continue;
                    }
                }

                //checks if the trip is already geofence to geofence locations
                if (_startLocation[1].Substring(0, 1) == "[" && _endLocation[1].Substring(0, 1) == "[")
                {
                    _StopsCount = 0;
                    _endTime = _endLocation[0];
                    _startTime = _startLocation[0];
                    _TotalTripLength = (decimal)data.TripLength;
                    _TotalTripDuration = (decimal)data.TravelTime;
                    _TotalIdleDuration = data.IdleDuration;
                    _TotalStopDuration = data.StopDuration;
                    _SpottedEndLocation = _endLocation[1];
                    _SpottedStartLocation = _startLocation[1];

                }
                
                //checks if the trip is within the time frame chosen by user
                int _startTimeHour = 0;
                int _startTimeMin = 0;
                int _startTimeSec = 0;
                if (data.MovementStart.Substring(0, 2) != "00")
                    _startTimeHour = Convert.ToInt32(data.MovementStart.Substring(0, 2).TrimStart(new char[] { '0' }));
                if (data.MovementStart.Substring(3, 2) != "00")
                    _startTimeMin = Convert.ToInt32(data.MovementStart.Substring(3, 2).TrimStart(new char[] { '0' }));
                if (data.MovementStart.Substring(6, 2) != "00")
                    _startTimeSec = Convert.ToInt32(data.MovementStart.Substring(6, 2).TrimStart(new char[] { '0' }));

                //continue means go to next loop, don't save the trip data
                if (_startTimeHour >= startTime && _startTimeHour <= endTime)
                {
                    if (_startTimeHour == endTime && _startTimeMin > 0 && _startTimeSec > 0)
                        continue;
                }
                else
                    continue;
                #endregion

                //Save data
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
                
                ResetTripValues(ref _StopsCount, ref _SpottedStartLocation, ref _SpottedEndLocation, ref _startTime, ref _endTime, ref _TotalTripLength, ref _TotalTripDuration, ref _TotalIdleDuration, ref _TotalStopDuration);
                TripReportRawList.Add(curData);
            }
        }
        private void ResetTripValues(ref int _StopsCount, ref string _SpottedStartLocation, ref string _SpottedEndLocation, ref string _startTime, ref string _endTime,
                                    ref decimal _TotalTripLength, ref decimal _TotalTripDuration, ref decimal? _TotalIdleDuration,  ref decimal? _TotalStopDuration)
        {
            _StopsCount = 0;
            _SpottedEndLocation = "";
            _SpottedStartLocation = "";
            _endTime = "";
            _startTime = "";
            _TotalTripLength = 0;
            _TotalTripDuration = 0;
            _TotalIdleDuration = 0;
            _TotalStopDuration = 0;
        }

        public void IdentifyVehicleUsage(List<TripsReportRaw> TripReportRawList, ref List<VehicleUsage> VehicleUsageSummary)
        {
            //get summary per distinct vehicles
            VehicleUsageSummary = TripReportRawList.GroupBy(s => s.EquipmentNo).Select(s => new VehicleUsage {
                                                                                                    VehicleID = s.Key,
                                                                                                    VehicleModel = s.Select(t => t.EquipmentType).First(),
                                                                                                    NumberOfTrips = s.Count(),
                                                                                                    TotalMileage = s.Sum(t => t.TripLength),
                                                                                                    AverageMileage = s.Sum(t => t.TripLength) / s.Count(),
                                                                                                    TotalTravelTimeDec = s.Sum(t => t.TripDurationDec),
                                                                                                    TotalTravelTime = ConvertToTimeFormat(Convert.ToDecimal(s.Sum(t => t.TripIdlingTimeDec))),
                                                                                                    AverageTravelTime = ConvertToTimeFormat(Convert.ToDecimal(s.Sum(t => t.TripDurationDec) / s.Count()))
                                                                                            }).OrderBy(s => s.VehicleID).ToList();
        }

        public void IdentifyVUByTimeRange(List<TripsReportRaw> TripReportRawList, ref List<UsageByTimeRange> VUByTimeRangeSummary)
        {
            //get summary per time range
            VUByTimeRangeSummary = TripReportRawList.GroupBy(s => s.StartTime.Substring(0, 2)).Select(s => new UsageByTimeRange {
                                                                                                    TimeRange = s.Key.Substring(0, 2) + ":00-" + s.Key.Substring(0, 2) + ":59",
                                                                                                    NumberOfTrips = s.Count(),
                                                                                                    TotalMileage = s.Sum(t => t.TripLength),
                                                                                                    AverageMileage = s.Sum(t => t.TripLength) / s.Count(),
                                                                                                    TotalTravelTimeDec = s.Sum(t => t.TripDurationDec),
                                                                                                    TotalTravelTime = ConvertToTimeFormat(Convert.ToDecimal(s.Sum(t => t.TripDurationDec))),
                                                                                                    AverageTravelTime = ConvertToTimeFormat(Convert.ToDecimal(s.Sum(t => t.TripDurationDec) / s.Count()))
                                                                                            }).OrderBy(s => s.TimeRange).ToList();
        }

        public void IdentifyVUHighlights(List<TripsReportRaw> TripReportRawList, int daysInBetween, ref VehicleUsageHighlights VUHighlights)
        {
            var MostTrips = TripReportRawList.GroupBy(s => s.EquipmentNo).OrderByDescending(t => t.Count()).Select(u => new { Vehicle = u.Key, Trips = u.Count() }).First();

            var TimeRangeMostTrips = TripReportRawList.GroupBy(s => s.StartTime.Substring(0, 2)).Select(u => new { Timerange = u.Key, Trips = u.Count() }).OrderByDescending(s => s.Trips).First();

            var TimeRangeMostVehicles = TripReportRawList.GroupBy(s => s.StartTime.Substring(0, 2))
                                                            .Select(u => new { Timerange = u.Key, Vehicles = u.Select(v => v.EquipmentNo).Distinct().Count() })
                                                            .OrderByDescending(s => s.Vehicles).First();
            
            VehicleUsageHighlights curHighlights = TripReportRawList.GroupBy(s => 1).Select(s => new VehicleUsageHighlights {
                                                                                                TotalMileage = s.Sum(t => t.TripLength),
                                                                                                AverageMileage = s.Sum(t => t.TripLength) / daysInBetween,
                                                                                                TotalTravelTime = s.Sum(t => t.TripDurationDec),
                                                                                                AverageTravelTime = s.Sum(t => t.TripDurationDec) / daysInBetween,
                                                                                                VehiclesUsed = s.Select(t => t.EquipmentNo).Distinct().Count(),
                                                                                                VehiclesNotUsed = db.RTNVehicleLists.Count() - s.Select(t => t.EquipmentNo).Distinct().Count()
                                                                                            }).First();

            VUHighlights.TotalTrips = TripReportRawList.Count();
            VUHighlights.AverageTrips = TripReportRawList.Count() / daysInBetween;

            VUHighlights.VehicleMostTrips = MostTrips.Vehicle;
            VUHighlights.VehicleNoOfTrips = MostTrips.Trips;

            VUHighlights.TimeRangeMostTrips = TimeRangeMostTrips.Timerange + ":00-" + TimeRangeMostTrips.Timerange + ":59";
            VUHighlights.TimeRangeNoOfTrips = TimeRangeMostTrips.Trips;

            VUHighlights.TimeRangeMostVehicles = TimeRangeMostVehicles.Timerange + ":00-" + TimeRangeMostVehicles.Timerange + ":59";
            VUHighlights.TimeRangeNoOfVehicles = TimeRangeMostVehicles.Vehicles;

            VUHighlights.TotalMileage = curHighlights.TotalMileage;
            VUHighlights.AverageMileage = curHighlights.AverageMileage;
            VUHighlights.TotalTravelTime = Math.Round((curHighlights.TotalTravelTime / 60) / 60, 2);
            VUHighlights.AverageTravelTime = Math.Round((curHighlights.AverageTravelTime / 60) / 60, 2);
            VUHighlights.VehiclesUsed = curHighlights.VehiclesUsed;
            VUHighlights.VehiclesNotUsed = curHighlights.VehiclesNotUsed;
        }

        public void IdentifyIdlingViolations(DateTime? DateFrom, DateTime? DateTo, int? StartTime, int? EndTime, List<ExcessiveIdling_vw> IdlistListDB, ref NonCompliantHighlights NCHighlights)
        {
            List<ExcessiveIdling_vw> curIdlingList = IdlistListDB.Where(s => Convert.ToInt32(s.TripTimeStart.Substring(0, 2)) >= StartTime &&
                                                                                Convert.ToInt32(s.TripTimeStart.Substring(0, 2)) <= EndTime).ToList();

            if (curIdlingList.Count() > 0)
            {
                var IdlingListByVehicle = curIdlingList.GroupBy(s => s.Header).Select(sg => new { Vehicle = sg.Key, IdlingCount = sg.Count() }).OrderByDescending(s => s.IdlingCount).First();
                var IdlingListByLocation = curIdlingList.GroupBy(s => s.Address).Select(sg => new { Address = sg.Key, IdlingCount = sg.Count() }).OrderByDescending(s => s.IdlingCount).First();
                var IdlingListByTimeRange = curIdlingList.GroupBy(s => s.TripTimeStart.Substring(0, 2)).Select(sg => new { TimeRange = sg.Key, IdlingCount = sg.Count() }).OrderByDescending(s => s.IdlingCount).First();
                
                NCHighlights.TotalIdlingCount = curIdlingList.Count();

                NCHighlights.VehicleMostIdling = IdlingListByVehicle.Vehicle;
                NCHighlights.VehicleMostIdlingCount = IdlingListByVehicle.IdlingCount;

                NCHighlights.TimeRangeMostIdling = IdlingListByTimeRange.TimeRange.Substring(0, 2) + ":00-" + IdlingListByTimeRange.TimeRange.Substring(0, 2) + ":59";
                NCHighlights.TimeRangeMostIdlingCount = IdlingListByTimeRange.IdlingCount;

                NCHighlights.LocationMostIdling = IdlingListByLocation.Address;
                NCHighlights.LocationMostIdlingCount = IdlingListByLocation.IdlingCount;
            }
        }

        public void IdentifySpeedingViolations(DateTime? DateFrom, DateTime? DateTo, int? StartTime, int? EndTime, List<SpeedViolation_vw> SpeedingListDB, ref NonCompliantHighlights NCHighlights)
        {
            List<SpeedViolation_vw> curSpeedingList = SpeedingListDB.Where(s => Convert.ToInt32(s.StartTime.Substring(0, 2)) >= Convert.ToInt32(StartTime) &&
                                                                                Convert.ToInt32(s.StartTime.Substring(0, 2)) <= Convert.ToInt32(EndTime)).ToList();

            if (curSpeedingList.Count() > 0)
            {
                var SpeedingListByVehicle = curSpeedingList.GroupBy(s => s.EquipmentID).Select(sg => new { Vehicle = sg.Key, SpeedingCount = sg.Count() }).OrderByDescending(s => s.SpeedingCount).First();
                var SpeedingListByLocation = curSpeedingList.GroupBy(s => s.Address).Select(sg => new { Address = sg.Key, SpeedingCount = sg.Count() }).OrderByDescending(s => s.SpeedingCount).First();
                var SpeedingListByTimeRange = curSpeedingList.GroupBy(s => s.StartTime.Substring(0, 2)).Select(sg => new { TimeRange = sg.Key, SpeedingCount = sg.Count() }).OrderByDescending(s => s.SpeedingCount).First();

                NCHighlights.TotalSpeedingCount = curSpeedingList.Count();

                NCHighlights.VehicleMostSpeeding = SpeedingListByVehicle.Vehicle;
                NCHighlights.VehicleMostSpeedingCount = SpeedingListByVehicle.SpeedingCount;

                NCHighlights.TimeRangeMostSpeeding = SpeedingListByTimeRange.TimeRange.Substring(0, 2) + ":00-" + SpeedingListByTimeRange.TimeRange.Substring(0, 2) + ":59";
                NCHighlights.TimeRangeMostSpeedingCount = SpeedingListByTimeRange.SpeedingCount;

                NCHighlights.LocationMostSpeeding = SpeedingListByLocation.Address;
                NCHighlights.LocationMostSpeedingCount = SpeedingListByLocation.SpeedingCount;
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