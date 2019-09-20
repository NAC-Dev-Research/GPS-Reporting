using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GPSReporting.Models
{
    public class ExcessiveIdling_vw
    {
        public int ID { get; set; }
        public int TrackerID { get; set; }
        public string Site { get; set; }
        public DateTime ReportDateFrom { get; set; }
        public string Header { get; set; }
        public string VehicleModel { get; set; }
        public string TripTimeStart { get; set; }
        public string TripTimeEnd { get; set; }
        public string Address { get; set; }
        public string StopDuration { get; set; }
        public decimal StopDurationDec { get; set; }
        public decimal IdlingDurationDec { get; set; }
        public string IdlingDuration { get; set; }
        public string IdlingPercent { get; set; }
    }
}