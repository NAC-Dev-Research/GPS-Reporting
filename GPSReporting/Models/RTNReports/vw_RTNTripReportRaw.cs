using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GPSReporting.Models.RTNReports
{
    public class vw_RTNTripReportRaw
    {
        public Int64 NID { get; set; }
        public int TrackerID { get; set; }
        public string EquipmentID { get; set; }
        public string EquipmentType { get; set; }
        public string VehicleModel { get; set; }
        public string PlateNumber { get; set; }
        public DateTime ReportDateFrom { get; set; }
        public DateTime ReportDateTo { get; set; }
        public Nullable<decimal> AverageSpeed { get; set; }
        public Nullable<decimal> TripLength { get; set; }
        public Nullable<decimal> MaxSpeed { get; set; }
        public Nullable<decimal> TravelTime { get; set; }
        public string MovementStart { get; set; }
        public string MovementEnd { get; set; }
        public string StopTimeFormat { get; set; }
        public Nullable<decimal> StopDuration { get; set; }
        public string IdleTimeFormat { get; set; }
        public Nullable<decimal> IdleDuration { get; set; }
    }
}