using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GPSReporting.Models
{
    public class TripsReportRawData
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Date { get; set; }
        public string EquipmentNo { get; set; }
        public string AssignedDriver { get; set; }
        public string OBTripNo { get; set; }
        public string TripName { get; set; }
        public string StartLocation { get; set; }
        public string EndLocation { get; set; }
        public string TripLength { get; set; }
        public string TripDuration { get; set; }
        public string UTStartLocation { get; set; }
        public string UTEndLocation { get; set; }
        public string EndOdometer { get; set; }
    }
}