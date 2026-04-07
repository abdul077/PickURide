using PickURide.Application.Models.AllRides;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models.Drivers
{
    public class LastRideDto
    {
        public Guid RideId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? DriverId { get; set; }
        public Guid? PassengerId { get; set; }
        public string? PassengerName { get; set; }
        public string? PassengerPhone { get; set; }

        public string? RideType { get; set; }
        public bool? IsScheduled { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public int? PassengerCount { get; set; }
        public decimal? FareEstimate { get; set; }
        public decimal? FareFinal { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }

        public double? Distance { get; set; }

        public TimeOnly RideStartTime { get; set; }
        public TimeOnly RideEndTime { get; set; }
        public TimeOnly TotalWaitingTime { get; set; }

        // Pickup/Drop-off info
        public string PickupLocation { get; set; } = string.Empty;
        public double PickupLat { get; set; }
        public double PickupLng { get; set; }
        public string DropOffLocation { get; set; } = string.Empty;
        public double DropOffLat { get; set; }
        public double DropOffLng { get; set; }

        public List<RideStopsDto> RideStops { get; set; } = new();

        public string PaymentIntentId { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
    }
}
