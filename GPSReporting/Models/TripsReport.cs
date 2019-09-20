using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GPSReporting.Models
{
    public class TripsReport
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string EquipmentName { get; set; }
        public int TotalTripsIdentified { get; set; }
        public int TotalTripsUnidentified { get; set; }
        public decimal TripsLengthTotal { get; set; }
        public decimal TripsLengthAverage { get; set; }
        public decimal TripsLengthMaximum { get; set; }
        public decimal TravelTimeTotal { get; set; }
        public decimal TravelTimeAverage { get; set; }
        public decimal TravelTimeMaximum { get; set; }
        public decimal ActivityAllocation { get; set; }
        public decimal EndOdometer { get; set; }
    }
}