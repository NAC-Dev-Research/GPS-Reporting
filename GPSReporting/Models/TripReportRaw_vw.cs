using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using PagedList;

namespace GPSReporting.Models
{
    public class TripReportRaw_vw
    {
        public int ID { get; set; }
        public int TrackerID { get; set; }
        public string Header { get; set; }
        public DateTime ReportDateFrom { get; set; }
        public decimal AverageSpeed { get; set; }
        public decimal TripLength { get; set; }
        public decimal TravelTime { get; set; }
        public string StartLocation { get; set; }
        public string EndLocation { get; set; }
        public string StopTimeFormat { get; set; }
        public decimal? StopDuration { get; set; }
        public string IdleTimeFormat { get; set; }
        public decimal? IdleDuration { get; set; }

        [DisplayFormat(DataFormatString = "{0:0,0}")]
        public decimal OdometerValue { get; set; }
    }

    public class TripsReportRaw
    {
        public string ReportDate { get; set; }
        public string EquipmentNo { get; set; }
        public string AssignedDriver { get; set; }
        public string OBTripNo { get; set; }
        public string TripName { get; set; }
        public string StartLocation { get; set; }
        public string EndLocation { get; set; }
        public decimal TripLength { get; set; }
        public string TripDuration { get; set; }
        public decimal TripDurationDec { get; set; }
        public string StopsInBetween { get; set; }
        public string TripIdentified { get; set; }
        public string StartOdometer { get; set; }
        public string EndOdometer { get; set; }
        public string TripIdlingTime { get; set; }
        public decimal? TripIdlingTimeDec { get; set; }
        public string TripStopTime { get; set; }
        public decimal? TripStopTimeDec { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }

    public class VehicleUsage
    {
        public string VehicleID { get; set; }
        public string VehicleModel { get; set; }
        public int NumberOfTrips { get; set; }
        public decimal TotalMileage { get; set; }
        public decimal AverageMileage { get; set; }
        public decimal TotalTravelTimeDec { get; set; }
        public string TotalTravelTime { get; set; }
        public string AverageTravelTime { get; set; }
    }

    public class UsageByTimeRange
    {
        public string TimeRange { get; set; }
        public int NumberOfTrips { get; set; }
        public decimal TotalMileage { get; set; }
        public decimal AverageMileage { get; set; }
        public decimal TotalTravelTimeDec { get; set; }
        public string TotalTravelTime { get; set; }
        public string AverageTravelTime { get; set; }

        public List<string> VehiclesList = new List<string>();
    }

    public class VehicleUsageHighlights
    {
        public string VehicleMostTrips { get; set; }
        public string VehicleType { get; set; }
        public int VehicleNoOfTrips { get; set; }
        public string TimeRangeMostVehicles { get; set; }
        public int TimeRangeNoOfVehicles { get; set; }
        public string TimeRangeMostTrips { get; set; }
        public int TimeRangeNoOfTrips { get; set; }
        public int TotalTrips { get; set; }
        public decimal AverageTrips { get; set; }
        public decimal TotalMileage { get; set; }
        public decimal AverageMileage { get; set; }
        public decimal TotalTravelTime { get; set; }
        public decimal AverageTravelTime { get; set; }
        [DisplayFormat(DataFormatString = "{0:0,0}")]
        public int VehiclesUsed { get; set; }
        public int VehiclesNotUsed { get; set; }
    }

    public class KeyTrips
    {
        public int EntryID { get; set; }
        public string Date { get; set; }
        public string StartLocation { get; set; }
        public string EndLocation { get; set; }
        public string VehicleID { get; set; }
        public int NumberOfTrips { get; set; }
        public decimal ExpectedTravelTime { get; set; }
        public string AverageTravelTime { get; set; }
        public string AverageIdlingTime { get; set; }
        public decimal AverageIdlingTimeDec { get; set; }
        public string AverageStopTime { get; set; }
    }

    public class KeyTripsHighlights
    {
        public string RouteMostTrip { get; set; }
        public int RouteNoOfTrip { get; set; }
        public decimal AverageTravelTimeDec { get; set; }
        public string AverageTravelTime { get; set; }
        public string VehicleMostTrip { get; set; }
        public string VehicleMostTripModel { get; set; }
        public int VehicleNoOfTrip { get; set; }
        public string RouteMostIdling { get; set; }
        public string RouteIdlingTime { get; set; }
        public string VehicleMostIdling { get; set; }
        public string VehicleMostIdlingModel { get; set; }
        public string VehicleIdlingTime { get; set; }
    }

    public class NonCompliantHighlights
    {
        public int TotalIdlingCount { get; set; }
        public string VehicleMostIdling { get; set; }
        public int VehicleMostIdlingCount { get; set; }
        public string VehicleMostIdlingModel { get; set; }
        public string TimeRangeMostIdling { get; set; }
        public int TimeRangeMostIdlingCount { get; set; }
        public string LocationMostIdling { get; set; }
        public int LocationMostIdlingCount { get; set; }

        public int TotalSpeedingCount { get; set; }
        public string VehicleMostSpeeding { get; set; }
        public string VehicleMostSpeedingModel { get; set; }
        public int VehicleMostSpeedingCount { get; set; }
        public string TimeRangeMostSpeeding { get; set; }
        public int TimeRangeMostSpeedingCount { get; set; }
        public string LocationMostSpeeding { get; set; }
        public int LocationMostSpeedingCount { get; set; }
    }

}