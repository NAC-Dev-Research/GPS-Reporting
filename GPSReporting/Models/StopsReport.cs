using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GPSReporting.Models
{
    public class StopsReport
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string EquipmentGroup { get; set; }
        public string GeofenceNameDesignation { get; set; }
        public string Location { get; set; }
        public int TotalVisits { get; set; }
        public decimal StopsDurationAve { get; set; }
        public decimal StopsDurationMax { get; set; }
        public decimal StopsDurationMin { get; set; }
        public decimal StopsIgnitionOnAve { get; set; }
        public decimal StopsIgnitionOnMax { get; set; }
        public decimal StopsIgnitionOnMin { get; set; }
    }
}