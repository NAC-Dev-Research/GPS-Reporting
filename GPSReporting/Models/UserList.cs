using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GPSReporting.Models
{
    public class UserList
    {
        public int ID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Site { get; set; }
        public string Role { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}