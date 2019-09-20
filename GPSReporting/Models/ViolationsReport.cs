using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GPSReporting.Models
{
    public class ViolationsReport
    {
        public string EquipmentNo { get; set; }
        public decimal SpeedViolationMaxSpeedDiff { get; set; }
        public decimal SpeedViolationDuration { get; set; }
        public string SpeedViolationTime { get; set; }
        public string SpeedViolationLocation { get; set; }
        public decimal HarshAccelMaxSpeedDiff { get; set; }
        public string HarshAccelTime { get; set; }
        public string HarshAccelLocation { get; set; }
        public decimal HarshBrakingMaxSpeedDiff { get; set; }
        public string HarshBrakingTime { get; set; }
        public string HarshBrakingLocation { get; set; }
        public decimal ExcessiveIdlingDuration { get; set; }
        public string ExcessiveIdlingTime { get; set; }
        public string ExcessiveIdlingLocation { get; set; }
        public int TotalViolationCount { get; set; }
    }
}