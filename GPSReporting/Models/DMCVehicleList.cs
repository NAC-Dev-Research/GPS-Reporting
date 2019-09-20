using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GPSReporting.Models
{
    public class DMCVehicleList
    {
        public int ID { get; set; }
        public string TrackerID { get; set; }
        public string PlateNumber { get; set; }
        public string VehicleModel { get; set; }
        public string EquipmentID { get; set; }
    }
}