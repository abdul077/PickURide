using System;

namespace PickURide.Application.Models
{
    public class CompleteTransactionRequest
    {
        public Guid RideId { get; set; }
        public Guid UserId { get; set; }
        public Guid DriverId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime CompletedAt { get; set; }
        public decimal? AdminShare { get; set; }
        public decimal? DriverShare { get; set; }
        public decimal? TipAmount { get; set; }
        public string? PromoCode { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentToken { get; set; }
        public string? TransferId { get; set; }
    }
}

