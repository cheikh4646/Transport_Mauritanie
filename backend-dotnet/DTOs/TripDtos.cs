using System;
using System.Collections.Generic;

namespace BackendDotnet.DTOs
{
    public class CreateTripDto
    {
        public int BusId { get; set; }
        public string DepartureCity { get; set; } = string.Empty;
        public string ArrivalCity { get; set; } = string.Empty;
        public DateTime DepartureDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public decimal Price { get; set; }
    }

    public class TripDto
    {
        public int Id { get; set; }
        public int BusId { get; set; }
        public string BusNumber { get; set; } = string.Empty;
        public int BusCapacity { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string DepartureCity { get; set; } = string.Empty;
        public string ArrivalCity { get; set; } = string.Empty;
        public DateTime DepartureDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<int> BookedSeats { get; set; } = new List<int>();
    }
}
