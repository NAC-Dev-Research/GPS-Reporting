using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GPSReporting.Models
{
    public class UtilizationReport
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string EquipmentGroup { get; set; }
        public string ActivityName { get; set; }
        public string StartLocation { get; set; }
        public string EndLocation { get; set; }
        public string AvailabilityAllocation { get; set; }
        public string IdlingTimeAve { get; set; }
        public string IdlingTimeMax { get; set; }
        public string IdlingTimeMin { get; set; }
    }
}