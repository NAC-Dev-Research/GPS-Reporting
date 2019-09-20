using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GPSReporting.Models
{
    public class GeofenceTrafficAssessment
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string GeofenceNameDesignation { get; set; }
        public string Location { get; set; }
        public string TimeInterval { get; set; }
        public string EquipmetnInsideAve { get; set; }
        public string EquipmetnInsideMax { get; set; }
        public string EquipmetnInsideMin { get; set; }
        public string IdlingAverage { get; set; }
        public string IdlingMaximum { get; set; }
        public string IdlingTotalHours { get; set; }
    }
}