using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GPSReporting.Models
{
    public class RTNVehicleList
    {
        public int TrackerID { get; set; }
        public string EquipmentID { get; set; }
        public string VehicleModel { get; set; }
        public string PlateNumber { get; set; }
        public string EquipmentType { get; set; }
    }
}