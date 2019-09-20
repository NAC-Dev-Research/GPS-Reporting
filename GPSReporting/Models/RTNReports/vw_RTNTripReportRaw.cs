using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GPSReporting.Models.RTNReports
{
    public class vw_RTNTripReportRaw
    {
        public int TrackerID { get; set; }
        public string EquipmentID { get; set; }
        public string EquipmentType { get; set; }
        public string VehicleModel { get; set; }
        public string PlateModel { get; set; }
        public DateTime ReportDateFrom { get; set; }
        public DateTime ReportDateTo { get; set; }
        public decimal AverageSpeed { get; set; }
        public decimal TripLength { get; set; }
        public decimal MaxSpeed { get; set; }
        public decimal TravelTime { get; set; }
        public string MovementStart { get; set; }
        public string MovementEnd { get; set; }
        public string StopTimeFormat { get; set; }
        public decimal StopDuration { get; set; }
        public string IdleTimeFormat { get; set; }
        public decimal IdleDuration { get; set; }
    }
}