using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GPSReporting.Models
{
    public class LogList
    {
        public int ID { get; set; }
        public DateTime Date { get; set; }
        public string Day { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Details { get; set; }
        public string Login { get; set; }
        public string Logout { get; set; }
        public string Remarks { get; set; }
        
    }
}