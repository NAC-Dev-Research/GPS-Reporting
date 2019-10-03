using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GPSReporting.Models
{
    public class QueryDetails
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? StartTime { get; set; }
        public int? EndTime { get; set; }
        public string Site { get; set; }
        public string VehicleType { get; set; }
    }
}