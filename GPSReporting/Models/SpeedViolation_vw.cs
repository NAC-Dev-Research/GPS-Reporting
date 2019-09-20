using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GPSReporting.Models
{
    public class SpeedViolation_vw
    {
        public int ID { get; set; }
        public int TrackerID { get; set; }
        public string Site { get; set; }
        public string PlateNumber { get; set; }
        public string VehicleModel { get; set; }
        public string EquipmentID { get; set; }
        public DateTime ReportDateFrom { get; set; }
        public string StartTime { get; set; }
        public string Duration { get; set; }
        public decimal DurationDec { get; set; }
        public int AverageSpeed { get; set; }
        public int MaxSpeed { get; set; }
        public string Address { get; set; }
    }
}