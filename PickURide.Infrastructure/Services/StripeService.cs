using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PickURide.Application.Interfaces.Services;
using Stripe;

namespace PickURide.Infrastructure.Services
{
    public class StripeService : IStripeService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripeService> _logger;

        public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            // Initialize Stripe API key
            var secretKey = _configuration["Stripe:SecretKey"];
            if (!string.IsNullOrEmpty(secretKey))
            {
                StripeConfiguration.ApiKey = secretKey;
            }
        }

        public async Task<TransferResult> TransferToDriverAsync(string paymentIntentId, string driverStripeAccountId, decimal totalAmount, decimal adminFee, decimal driverAmount, Guid rideId)
        {
            try
            {
                if (string.IsNullOrEmpty(driverStripeAccountId))
                {
                    return new TransferResult
                    {
                        Success = false,
                        ErrorMessage = "Driver does not have a Stripe account configured."
                    };
                }

                if (string.IsNullOrEmpty(paymentIntentId))
                {
                    return new TransferResult
                    {
                        Success = false,
                        ErrorMessage = "Payment Intent ID is required."
                    };
                }

                // Convert amounts to cents (Stripe uses smallest currency unit)
                long driverAmountCents = (long)(driverAmount * 100);
                long totalAmountCents = (long)(totalAmount * 100);

                _logger.LogInformation("Creating Stripe transfer for ride {RideId}. PaymentIntentId: {PaymentIntentId}, DriverAccountId: {DriverAccountId}, DriverAmount: {DriverAmount}", 
                    rideId, paymentIntentId, driverStripeAccountId, driverAmountCents);

                // Create transfer using source_transaction to link to the payment intent
                var transferOptions = new TransferCreateOptions
                {
                    Amount = driverAmountCents,
                    Currency = "cad",
                    Destination = driverStripeAccountId,
                    SourceTransaction = paymentIntentId, // Link to the original payment
                    Description = $"Ride payment for {rideId}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "rideId", rideId.ToString() },
                        { "paymentIntentId", paymentIntentId },
                        { "adminFee", adminFee.ToString("F2") },
                        { "adminFeePercentage", ((adminFee / totalAmount) * 100).ToString("F2") }
                    }
                };

                var transferService = new TransferService();
                var transfer = await transferService.CreateAsync(transferOptions);

                _logger.LogInformation("Stripe transfer created successfully. TransferId: {TransferId}, RideId: {RideId}", transfer.Id, rideId);

                return new TransferResult
                {
                    Success = true,
                    TransferId = transfer.Id
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe API error while creating transfer for ride {RideId}. Stripe Error: {StripeError}", rideId, ex.Message);
                return new TransferResult
                {
                    Success = false,
                    ErrorMessage = $"Stripe error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating Stripe transfer for ride {RideId}", rideId);
                return new TransferResult
                {
                    Success = false,
                    ErrorMessage = $"Error creating transfer: {ex.Message}"
                };
            }
        }
    }
}

