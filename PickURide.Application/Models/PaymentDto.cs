using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class PaymentDto
    {
        public Guid PaymentId { get; set; }
        public Guid? RideId { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal? PaidAmount { get; set; }
        public decimal? TipAmount { get; set; }
        public decimal? AdminShare { get; set; }
        public decimal? DriverShare { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime? CreatedAt { get; set; }
        public Guid? UserId { get; set; }
        public Guid? DriverId { get; set; }
        public string? PaymentToken { get; set; }
        public string? PromoCode { get; set; }
        public string? TransferStatus { get; set; }
        public string? TransferId { get; set; }
        public DateTime? TransferredAt { get; set; }
        public decimal? TotalAmount { get; set; }
    }

    public class HeldPaymentDto
    {
        public Guid? RideId { get; set; }
        public Guid UserId { get; set; }
        public string? PaymentIntentId { get; set; }
        public decimal HeldAmount { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateHeldPaymentRequest
    {
        public Guid? RideId { get; set; }
        public Guid UserId { get; set; }
        public string? PaymentIntentId { get; set; }
        public decimal HeldAmount { get; set; }
        public Guid? DriverId { get; set; }
        public string? PaymentMethod { get; set; }
    }
}
