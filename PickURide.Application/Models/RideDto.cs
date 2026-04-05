using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class RideDto
    {
        public Guid RideId { get; set; }
        public Guid UserId { get; set; }
        public Guid? DriverId { get; set; }
        public string UserName { get; set; }
        public string DriverName { get; set; }
        public string RideType { get; set; }
        public string Vehicle { get; set; }
        public string VehicleColor { get; set; }
        public bool IsScheduled { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public int PassengerCount { get; set; }
        public decimal FareEstimate { get; set; }
        public decimal? FareFinal { get; set; }
		public decimal? TipAmount { get; set; }
        public string Status { get; set; }
        public double Distance { get; set; }
        public string AdminCommission { get; set; }
        public string DriverPayment { get; set; }
        public string PickupLocation { get; set; }
        public string DropoffLocation { get; set; }
        public TimeOnly? TotalWaitingTime { get; set; }
        public TimeOnly? RideStartTime { get; set; }
        public TimeOnly? RideEndTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? PromoCode { get; set; }
        public decimal? PromoDiscount { get; set; }
        public List<RideStopDto> RideStops { get; set; } = new();
    }
}
