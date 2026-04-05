using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class BookRideRequest
    {
        public Guid UserId { get; set; }
        public string RideType { get; set; } = "Standard";
        public bool IsScheduled { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public int PassengerCount { get; set; }
        public decimal FareEstimate { get; set; }
        public string? PromoCode { get; set; }
        /// <summary>
        /// Payment Intent ID from Stripe (sent by driver)
        /// </summary>
        public string? PaymentToken { get; set; }
        /// <summary>
        /// Payment status (e.g., "pending", "completed") - sent by driver
        /// </summary>
        public string? PaymentStatus { get; set; }
        /// <summary>
        /// Transfer status (e.g., "pending", "completed") - sent by driver
        /// </summary>
        public string? TransferStatus { get; set; }
        public List<RideStopDto> Stops { get; set; }
    }
    public class RideStopDto
    {
        public Guid RideStopId { get; set; }
        public Guid RideId { get; set; }
        public int StopOrder { get; set; }
        public string Location { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
