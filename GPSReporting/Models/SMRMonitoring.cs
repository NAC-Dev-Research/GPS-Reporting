using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GPSReporting.Models
{
    public class SMRMonitoring
    {
        public string EquipmentNo { get; set; }
        public string LatestPMDate { get; set; }
        public decimal TotalDistanceTravelled { get; set; }
        public decimal EngineHours { get; set; }
        public string NextPMSMR { get; set; }
    }
}