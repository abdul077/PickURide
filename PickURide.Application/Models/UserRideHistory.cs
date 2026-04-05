using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class UserRideHistory
    {
        public Guid RideId { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public decimal? FareFinal { get; set; }
		public decimal? TipAmount { get; set; }
        public string Status { get; set; }
        public double Distance { get; set; }
        public string PickupLocation { get; set; }
        public string DropoffLocation { get; set; }
        public TimeOnly? RideStartTime { get; set; }
        public TimeOnly? RideEndTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public string PaymentIntentId { get; set; }
        public string PaymentStatus { get; set; }
        public Guid? DriverId { get; set; }
        public string DriverName { get; set; }
        public string DriverPhoneNumber { get; set; }
        public string VehicleName { get; set; }
        public string VehicleColor { get; set; }
    }
}
