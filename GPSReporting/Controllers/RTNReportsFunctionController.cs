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

        public List<TripsReportRaw> IdentifyTripsRTN(DateTime? dateFrom, DateTime? dateTo, List<TripsReportRaw> TripReportRawList)
        {
            List<vw_RTNTripReportRaw> tripReportRawDB = db.vw_RTNTripReportRaw.Where(s => s.ReportDateFrom >= dateFrom && s.ReportDateFrom <= dateTo).OrderBy(s => s.TrackerID).ThenBy(s => s.ReportDateFrom).ThenBy(s => s.MovementStart).ToList();

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
                    ResetTripValues(ref _StopsCount, ref _SpottedStartLocation, ref _SpottedEndLocation, ref _startTime, ref _endTime, ref _TotalTripLength, ref _TotalTripDuration, ref _TotalIdleDuration, ref _TotalStopDuration);
                }

                string[] _startLocation = new string[2] { "", "" };
                string[] _endLocation = new string[2] { "", "" };
                _startLocation = data.MovementStart.ToString().Split(new string[] { " - " }, StringSplitOptions.None);
                _endLocation = data.MovementEnd.ToString().Split(new string[] { " - " }, StringSplitOptions.None);

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
                    _TotalIdleDuration = data.IdleDuration;
                    _TotalStopDuration = data.StopDuration;
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
                
                ResetTripValues(ref _StopsCount, ref _SpottedStartLocation, ref _SpottedEndLocation, ref _startTime, ref _endTime, ref _TotalTripLength, ref _TotalTripDuration, ref _TotalIdleDuration, ref _TotalStopDuration);
                TripReportRawList.Add(curData);
            }
            return TripReportRawList;
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