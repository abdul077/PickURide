using PickURide.Application.Models;

namespace PickURide.Application.Interfaces.Services
{
    public interface IStripeService
    {
        Task<TransferResult> TransferToDriverAsync(string paymentIntentId, string driverStripeAccountId, decimal totalAmount, decimal adminFee, decimal driverAmount, Guid rideId);
    }

    public class TransferResult
    {
        public bool Success { get; set; }
        public string? TransferId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

