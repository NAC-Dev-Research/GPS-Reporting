using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GPSReporting.Models
{
    public class ReportsViewModel
    {
        public ReportsViewModel()
        {
        }
        public static ReportsViewModel obj;
        private static readonly object myLockObject = new object();

        public static ReportsViewModel GetInstance()
        {
            lock (myLockObject)
            {
                if (obj == null)
                    obj = new ReportsViewModel();
            }
            return obj;
        }
        
        public List<TripsReportRaw> _tripReportRaw               = new List<TripsReportRaw>();
        public List<VehicleUsage> _vehicleUsageList              = new List<VehicleUsage>();
        public List<UsageByTimeRange> _usageByTimeRangeList      = new List<UsageByTimeRange>();
        public List<KeyTrips> _keyTripsList                      = new List<KeyTrips>();
        public List<KeyTrips> _keyTripsListCopy                  = new List<KeyTrips>();
        public List<SpeedViolation_vw> _overSpeedingList         = new List<SpeedViolation_vw>();
        public List<SpeedViolation_vw> _overSpeedingListCopy     = new List<SpeedViolation_vw>();
        public List<ExcessiveIdling_vw> _excessiveIdlingList     = new List<ExcessiveIdling_vw>();
        public List<ExcessiveIdling_vw> _excessiveIdlingListCopy = new List<ExcessiveIdling_vw>();
        public List<string> _DMCVehicleList                      = new List<string>();
        public List<string> _DMCVehicleNotUsedList               = new List<string>();
        public List<TripsReportRaw> _chosenVehicleUsage          = new List<TripsReportRaw>();

        public VehicleUsageHighlights UsageHighlights            = new VehicleUsageHighlights();
        public KeyTripsHighlights KeyTripsHighlights             = new KeyTripsHighlights();
        public NonCompliantHighlights NoncompliantHighlights     = new NonCompliantHighlights();
        public UserList UserDetails                              = new UserList();
        public LogList LogDetails                                = new LogList();

        public string ReportType { get; set; }
        public DateTime? DateFROM { get; set; }
        public DateTime? DateTO { get; set; }
        public int? StartTime { get; set; }
        public int? EndTime { get; set; }
        public int DaysInBetween { get; set; }
        public string CurrentWindow { get; set; }
        public string CurrentUser { get; set; }
        public string PageTitle { get; set; }
        public string VehicleID { get; set; }
    }
}