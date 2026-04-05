using PickURide.Application.Models;
using PickURide.Application.Models.Drivers;

namespace PickURide.Application.Interfaces.Repositories
{
    public interface IPaymentRepository
    {
        Task AddAsync(PaymentDto payment);
        Task UpdateAsync(PaymentDto payment);
        Task<PaymentDto> GetByRideIdAsync(Guid rideId);
        Task<IEnumerable<PaymentDto>> GetAllAsync();
        Task<IEnumerable<PaymentDetailDto>> GetAllWithDetailsAsync();
        Task<PaymentPagedResultDto> GetPagedPaymentsWithDetailsAsync(PaymentFilterDto filter);
        Task<PaymentEarningsSummaryDto> GetEarningsSummaryAsync();
        Task<string> AddCustomerPayment(CustomerPaymentDto payment);
        Task<DriverTransactionDto> DriverTransaction(Guid driverId);
        Task<DriverTransactionDto> DriverPaidTransactionDetails(Guid driverId);
        Task UpdateTransferStatusAsync(Guid rideId, string transferStatus, string transferId, DateTime? transferredAt);
        Task<PaymentDto> CompleteTransactionAsync(CompleteTransactionRequest request);
        Task<IEnumerable<HeldPaymentDto>> GetHeldPaymentsAsync();
        Task<HeldPaymentDto> CreateHeldPaymentAsync(CreateHeldPaymentRequest request);
    }
}
