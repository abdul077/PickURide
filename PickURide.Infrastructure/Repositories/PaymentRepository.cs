using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Models;
using PickURide.Application.Models.Drivers;
using PickURide.Infrastructure.Data;
using PickURide.Infrastructure.Data.Entities;
using PickURide.Infrastructure.Hub;
using System;
using System.Linq;

namespace PickURide.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly PickURideDbContext _context;
        private readonly IHubContext<RideChatHub> _hubContext;
        private readonly IUnitOfWork _unitOfWork;
        public PaymentRepository(PickURideDbContext context, IHubContext<RideChatHub> hubContext, IUnitOfWork unitOfWork)
        {
            _context = context;
            _hubContext = hubContext;
            _unitOfWork = unitOfWork;
        }

        public async Task AddAsync(PaymentDto payment)
        {
            Payment paymentEntity = new Payment
            {
                PaymentId = Guid.NewGuid(),
                RideId = payment.RideId,
                AdminShare = payment.AdminShare,
                DriverShare = payment.DriverShare,
                PaidAmount = payment.PaidAmount,
                PaymentStatus = string.IsNullOrWhiteSpace(payment.PaymentStatus) ? "Pending" : payment.PaymentStatus,
                TipAmount = payment.TipAmount,
                PaymentMethod = payment.PaymentMethod,
                CreatedAt = payment.CreatedAt ?? DateTime.UtcNow,
                UserId = payment.UserId,
                DriverId = payment.DriverId,
                PaymentToken = payment.PaymentToken,
                TransferStatus = string.IsNullOrWhiteSpace(payment.TransferStatus) ? "pending" : payment.TransferStatus,
                TransferId = payment.TransferId,
                TransferredAt = payment.TransferredAt
            };
            await _context.Payments.AddAsync(paymentEntity);

            // Update the Ride's FareFinal to match the payment amount
            if (payment.RideId.HasValue && payment.PaidAmount.HasValue)
            {
                var ride = await _context.Rides.FirstOrDefaultAsync(r => r.RideId == payment.RideId.Value);
                if (ride != null)
                {
                    ride.FareFinal = payment.PaidAmount.Value;
                    _context.Rides.Update(ride);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<string> AddCustomerPayment(CustomerPaymentDto payment)
        {
            var paymentEntity = await _context.Payments
                .Where(m => m.RideId == payment.RideId)
                .FirstOrDefaultAsync();

            if (paymentEntity != null)
            {
                paymentEntity.DriverId = payment.DriverId;
                paymentEntity.UserId = payment.UserId;
                paymentEntity.CreatedAt = DateTime.UtcNow;
                paymentEntity.PaymentToken = payment.PaymentToken;
                paymentEntity.TipAmount = payment.TipAmount;
                paymentEntity.CustomerPaid = payment.PaidAmount.ToString();

                // Declare variables in outer scope
                Ride? ride = null;
                RideStop? pickupStop = null;
                string pickupLocation = "";

                // Recalculate AdminShare and DriverShare from fare configuration if not set correctly
                if (paymentEntity.AdminShare == 0 || paymentEntity.DriverShare == 0 || 
                    (paymentEntity.AdminShare + paymentEntity.DriverShare) != paymentEntity.PaidAmount)
                {
                    ride = await _context.Rides
                        .Include(r => r.RideStops)
                        .FirstOrDefaultAsync(r => r.RideId == payment.RideId);
                    
                    if (ride != null && ride.FareFinal.HasValue && ride.FareFinal.Value > 0)
                    {
                        pickupStop = ride.RideStops?.OrderBy(s => s.StopOrder).FirstOrDefault();
                        if (pickupStop != null)
                        {
                            pickupLocation = pickupStop.Location ?? "";
                            var allFareSettings = await _unitOfWork.FareSettingRepository.GetAllFareSettingsAsync();
                            var fareSettings = allFareSettings
                                .FirstOrDefault(f => !string.IsNullOrEmpty(f.AreaType) &&
                                                     pickupLocation.Contains(f.AreaType, StringComparison.OrdinalIgnoreCase));

                            if (fareSettings != null && fareSettings.AdminPercentage.HasValue)
                            {
                                decimal finalFare = ride.FareFinal.Value;
                                decimal adminCommissionPercent = fareSettings.AdminPercentage.Value;
                                decimal adminCommission = finalFare * adminCommissionPercent / 100m;
                                decimal driverPayment = finalFare - adminCommission;

                                // Round to whole numbers
                                adminCommission = Math.Round(adminCommission, 0, MidpointRounding.AwayFromZero);
                                driverPayment = Math.Round(driverPayment, 0, MidpointRounding.AwayFromZero);

                                paymentEntity.AdminShare = adminCommission;
                                paymentEntity.DriverShare = driverPayment;
                            }
                        }
                    }
                }

                _context.Payments.Update(paymentEntity);
                await _context.SaveChangesAsync();

                // Load ride if not already loaded
                if (ride == null)
                {
                    ride = await _context.Rides.Include(r => r.RideStops).FirstOrDefaultAsync(r => r.RideId == payment.RideId);
                }
                
                var user= await _context.Users.FirstOrDefaultAsync(u => u.UserId == payment.UserId);
                var requestRideStops = await _unitOfWork.RideRepository.GetRideStops(payment.RideId??Guid.Empty);
                var dropoffStop = requestRideStops.OrderByDescending(s => s.StopOrder).FirstOrDefault();
                var orderedStops = requestRideStops.OrderBy(s => s.StopOrder).ToList();

                // Get pickupStop if not already loaded
                if (pickupStop == null && ride != null)
                {
                    pickupStop = ride.RideStops?.OrderBy(s => s.StopOrder).FirstOrDefault();
                }
                
                if (pickupStop == null)
                    return "Pickup stop not found.";

                // Set pickupLocation if not already set
                if (string.IsNullOrEmpty(pickupLocation))
                {
                    pickupLocation = pickupStop.Location ?? "";
                }

                await _hubContext.Clients.Group(payment.DriverId.ToString())
              .SendAsync("NewRideAssigned", new
              {
                  ride.RideId,
                  ride.RideType,
                  ride.FareFinal,
                  ride.CreatedAt,
                  ride.Status,
                  PassengerId = user.UserId,
                  PassengerName = user.FullName,
                  PassengerPhone = user.PhoneNumber,
                  PickupLocation = pickupStop.Location,
                  PickUpLat = pickupStop.Latitude,
                  PickUpLon = pickupStop.Longitude,
                  DropoffLocation = dropoffStop?.Location,
                  DropoffLat = dropoffStop?.Latitude,
                  DropoffLon = dropoffStop?.Longitude,
                  Stops = orderedStops.Select(s => new
                  {
                      s.StopOrder,
                      s.Location,
                      s.Latitude,
                      s.Longitude
                  }).ToList(),
                  PassengerCount = ride.PassengerCount,
                  Payment= "Successful",
                  Tip=payment.TipAmount,
              });

                //// ✅ Prepare payload for SignalR broadcast
                //var payload = new
                //{
                //    RideId = payment.RideId,
                //    DriverId = payment.DriverId,
                //    UserId = payment.UserId,
                //    TipAmount = payment.TipAmount,
                //    TotalAmount = payment.PaidAmount,
                //    Message = "💳 Payment completed successfully!",
                //    Timestamp = DateTime.UtcNow
                //};

                //// ✅ Notify both driver and user
                //await _hubContext.Clients.Group(payment.DriverId.ToString())
                //    .SendAsync("PaymentCompleted", payload);

                //await _hubContext.Clients.Group(payment.UserId.ToString())
                //    .SendAsync("PaymentCompleted", payload);

                //// ✅ Also notify anyone viewing the ride (optional)
                //await _hubContext.Clients.Group(payment.RideId.ToString())
                //    .SendAsync("PaymentCompleted", payload);

                return "Payment updated and broadcast successfully";
            }
            else
            {
                return "Data does not exist";
            }
        }

        public async Task<DriverTransactionDto> DriverPaidTransactionDetails(Guid driverId)
        {
            var payments = await _context.Payments
                .Include(p => p.Ride)
                    .ThenInclude(r => r.RideStops)
                .Where(p => p.DriverId == driverId && p.PaymentStatus != null && p.PaymentStatus.ToLower() == "completed")
                .ToListAsync();

            var transactions = new List<PaymentDto>();
            var allFareSettings = await _unitOfWork.FareSettingRepository.GetAllFareSettingsAsync();

            foreach (var payment in payments)
            {
                // Recalculate AdminShare and DriverShare if incorrect
                decimal adminShare = payment.AdminShare ?? 0;
                decimal driverShare = payment.DriverShare ?? 0;
                decimal paymentPaidAmount = payment.PaidAmount ?? 0;

                if (adminShare == 0 || driverShare == 0 || (adminShare + driverShare) != paymentPaidAmount)
                {
                    if (payment.Ride != null && payment.Ride.FareFinal.HasValue && payment.Ride.FareFinal.Value > 0)
                    {
                        decimal finalFare = payment.Ride.FareFinal.Value;
                        bool calculated = false;

                        // First, try to use AdminCommission and DriverPayment from Ride table if available
                        if (!string.IsNullOrEmpty(payment.Ride.AdminCommission) && 
                            decimal.TryParse(payment.Ride.AdminCommission, out decimal rideAdminCommission) &&
                            rideAdminCommission > 0 &&
                            !string.IsNullOrEmpty(payment.Ride.DriverPayment) &&
                            decimal.TryParse(payment.Ride.DriverPayment, out decimal rideDriverPayment) &&
                            rideDriverPayment > 0)
                        {
                            adminShare = rideAdminCommission;
                            driverShare = rideDriverPayment;
                            calculated = true;
                        }
                        else
                        {
                            // Try to match fare settings by location
                            var pickupStop = payment.Ride.RideStops?.OrderBy(s => s.StopOrder).FirstOrDefault();
                            if (pickupStop != null)
                            {
                                string pickupLocation = pickupStop.Location ?? "";
                                var fareSettings = allFareSettings
                                    .FirstOrDefault(f => !string.IsNullOrEmpty(f.AreaType) &&
                                                         pickupLocation.Contains(f.AreaType, StringComparison.OrdinalIgnoreCase));

                                if (fareSettings != null && fareSettings.AdminPercentage.HasValue)
                                {
                                    decimal adminCommissionPercent = fareSettings.AdminPercentage.Value;
                                    adminShare = finalFare * adminCommissionPercent / 100m;
                                    driverShare = finalFare - adminShare;
                                    calculated = true;
                                }
                            }

                            // Fallback: Use first available fare setting's admin percentage if no match found
                            if (!calculated && allFareSettings.Any())
                            {
                                var defaultFareSetting = allFareSettings
                                    .FirstOrDefault(f => f.AdminPercentage.HasValue && f.AdminPercentage.Value > 0);
                                
                                if (defaultFareSetting != null && defaultFareSetting.AdminPercentage.HasValue)
                                {
                                    decimal adminCommissionPercent = defaultFareSetting.AdminPercentage.Value;
                                    adminShare = finalFare * adminCommissionPercent / 100m;
                                    driverShare = finalFare - adminShare;
                                    calculated = true;
                                }
                            }
                        }

                        if (calculated)
                        {
                            // Round to whole numbers
                            adminShare = Math.Round(adminShare, 0, MidpointRounding.AwayFromZero);
                            driverShare = Math.Round(driverShare, 0, MidpointRounding.AwayFromZero);

                            // Ensure they sum to the paid amount
                            if (adminShare + driverShare != paymentPaidAmount && paymentPaidAmount > 0)
                            {
                                // Adjust to match paid amount exactly
                                decimal ratio = paymentPaidAmount / finalFare;
                                adminShare = Math.Round(adminShare * ratio, 0, MidpointRounding.AwayFromZero);
                                driverShare = paymentPaidAmount - adminShare;
                            }

                            // Update the payment in database
                            payment.AdminShare = adminShare;
                            payment.DriverShare = driverShare;
                            _context.Payments.Update(payment);
                        }
                    }
                }

                transactions.Add(new PaymentDto
                {
                    PaymentId = payment.PaymentId,
                    RideId = payment.RideId,
                    AdminShare = payment.AdminShare,
                    DriverShare = payment.DriverShare,
                    PaidAmount = payment.PaidAmount,
                    TipAmount = payment.TipAmount,
                    PaymentMethod = payment.PaymentMethod,
                    PaymentStatus = payment.PaymentStatus,
                    CreatedAt = payment.CreatedAt,
                    UserId = payment.UserId,
                    DriverId = payment.DriverId,
                    PaymentToken = payment.PaymentToken,
                    TransferStatus = payment.TransferStatus,
                    TransferId = payment.TransferId,
                    TransferredAt = payment.TransferredAt,
                    TotalAmount = (payment.DriverShare ?? 0) + (payment.TipAmount ?? 0)
                });
            }

            await _context.SaveChangesAsync();

            var totalEarnings = transactions.Sum(t => t.DriverShare ?? 0);
            var totalTips = transactions.Sum(t => t.TipAmount ?? 0);
            var pendingAmount = transactions.Where(t => t.PaymentStatus != "Paid").Sum(t => t.DriverShare ?? 0);
            var paidAmount = totalEarnings - pendingAmount;
            var totalTrips = transactions.Count;
            var totalAdminShare = transactions.Sum(t => t.AdminShare ?? 0);
            var totalDriverShare = transactions.Sum(t => t.DriverShare ?? 0);
            var result = new DriverTransactionDto
            {
                TotalPayment = (totalEarnings + totalTips).ToString(),
                TotalTrips = totalTrips.ToString(),
                PaidAmount = paidAmount.ToString(),
                Payment = transactions
            };
            return result;
        }

        public async Task<DriverTransactionDto> DriverTransaction(Guid driverId)
        {
            var payments = await _context.Payments
                .Include(p => p.Ride)
                    .ThenInclude(r => r.RideStops)
                .Where(p => p.DriverId == driverId)
                .ToListAsync();

            var transactions = new List<PaymentDto>();
            var allFareSettings = await _unitOfWork.FareSettingRepository.GetAllFareSettingsAsync();

            foreach (var payment in payments)
            {
                // Recalculate AdminShare and DriverShare if incorrect
                decimal adminShare = payment.AdminShare ?? 0;
                decimal driverShare = payment.DriverShare ?? 0;
                decimal paymentPaidAmount = payment.PaidAmount ?? 0;

                if (adminShare == 0 || driverShare == 0 || (adminShare + driverShare) != paymentPaidAmount)
                {
                    if (payment.Ride != null && payment.Ride.FareFinal.HasValue && payment.Ride.FareFinal.Value > 0)
                    {
                        decimal finalFare = payment.Ride.FareFinal.Value;
                        bool calculated = false;

                        // First, try to use AdminCommission and DriverPayment from Ride table if available
                        if (!string.IsNullOrEmpty(payment.Ride.AdminCommission) && 
                            decimal.TryParse(payment.Ride.AdminCommission, out decimal rideAdminCommission) &&
                            rideAdminCommission > 0 &&
                            !string.IsNullOrEmpty(payment.Ride.DriverPayment) &&
                            decimal.TryParse(payment.Ride.DriverPayment, out decimal rideDriverPayment) &&
                            rideDriverPayment > 0)
                        {
                            adminShare = rideAdminCommission;
                            driverShare = rideDriverPayment;
                            calculated = true;
                        }
                        else
                        {
                            // Try to match fare settings by location
                            var pickupStop = payment.Ride.RideStops?.OrderBy(s => s.StopOrder).FirstOrDefault();
                            if (pickupStop != null)
                            {
                                string pickupLocation = pickupStop.Location ?? "";
                                var fareSettings = allFareSettings
                                    .FirstOrDefault(f => !string.IsNullOrEmpty(f.AreaType) &&
                                                         pickupLocation.Contains(f.AreaType, StringComparison.OrdinalIgnoreCase));

                                if (fareSettings != null && fareSettings.AdminPercentage.HasValue)
                                {
                                    decimal adminCommissionPercent = fareSettings.AdminPercentage.Value;
                                    adminShare = finalFare * adminCommissionPercent / 100m;
                                    driverShare = finalFare - adminShare;
                                    calculated = true;
                                }
                            }

                            // Fallback: Use first available fare setting's admin percentage if no match found
                            if (!calculated && allFareSettings.Any())
                            {
                                var defaultFareSetting = allFareSettings
                                    .FirstOrDefault(f => f.AdminPercentage.HasValue && f.AdminPercentage.Value > 0);
                                
                                if (defaultFareSetting != null && defaultFareSetting.AdminPercentage.HasValue)
                                {
                                    decimal adminCommissionPercent = defaultFareSetting.AdminPercentage.Value;
                                    adminShare = finalFare * adminCommissionPercent / 100m;
                                    driverShare = finalFare - adminShare;
                                    calculated = true;
                                }
                            }
                        }

                        if (calculated)
                        {
                            // Round to whole numbers
                            adminShare = Math.Round(adminShare, 0, MidpointRounding.AwayFromZero);
                            driverShare = Math.Round(driverShare, 0, MidpointRounding.AwayFromZero);

                            // Ensure they sum to the paid amount
                            if (adminShare + driverShare != paymentPaidAmount && paymentPaidAmount > 0)
                            {
                                // Adjust to match paid amount exactly
                                decimal ratio = paymentPaidAmount / finalFare;
                                adminShare = Math.Round(adminShare * ratio, 0, MidpointRounding.AwayFromZero);
                                driverShare = paymentPaidAmount - adminShare;
                            }

                            // Update the payment in database
                            payment.AdminShare = adminShare;
                            payment.DriverShare = driverShare;
                            _context.Payments.Update(payment);
                        }
                    }
                }

                transactions.Add(new PaymentDto
                {
                    PaymentId = payment.PaymentId,
                    RideId = payment.RideId,
                    AdminShare = payment.AdminShare,
                    DriverShare = payment.DriverShare,
                    PaidAmount = payment.PaidAmount,
                    TipAmount = payment.TipAmount,
                    PaymentMethod = payment.PaymentMethod,
                    PaymentStatus = payment.PaymentStatus,
                    CreatedAt = payment.CreatedAt,
                    UserId = payment.UserId,
                    DriverId = payment.DriverId,
                    PaymentToken = payment.PaymentToken,
                    TransferStatus = payment.TransferStatus,
                    TransferId = payment.TransferId,
                    TransferredAt = payment.TransferredAt,
                    TotalAmount = (payment.DriverShare ?? 0) + (payment.TipAmount ?? 0)
                });
            }

            await _context.SaveChangesAsync();

            var totalEarnings = transactions.Sum(t => t.DriverShare ?? 0);
            var totalTips = transactions.Sum(t => t.TipAmount ?? 0);
            var pendingAmount = transactions.Where(t => t.PaymentStatus != "Paid").Sum(t => t.DriverShare ?? 0);
            var paidAmount = totalEarnings - pendingAmount;
            var totalTrips = transactions.Count;
            var totalAdminShare = transactions.Sum(t => t.AdminShare ?? 0);
            var totalDriverShare = transactions.Sum(t => t.DriverShare ?? 0);
            var result = new DriverTransactionDto
            {
                TotalPayment = (totalEarnings + totalTips).ToString(),
                TotalTrips = totalTrips.ToString(),
                PaidAmount = paidAmount.ToString(),
                Payment = transactions
            };
            return result;
        }

        public Task<IEnumerable<PaymentDto>> GetAllAsync()
        {
            var payments = _context.Payments.Select(p => new PaymentDto
            {
                PaymentId = p.PaymentId,
                RideId = p.RideId,
                AdminShare = p.AdminShare,
                DriverShare = p.DriverShare,
                PaidAmount = p.PaidAmount,
                TipAmount = p.TipAmount,
                PaymentMethod = p.PaymentMethod,
                PaymentStatus = p.PaymentStatus,
                CreatedAt = p.CreatedAt,
                UserId = p.UserId,
                DriverId = p.DriverId,
                PaymentToken = p.PaymentToken,
                TransferStatus = p.TransferStatus,
                TransferId = p.TransferId,
                TransferredAt = p.TransferredAt,
                TotalAmount = (p.DriverShare ?? 0) + (p.TipAmount ?? 0)
            });
            return Task.FromResult<IEnumerable<PaymentDto>>(payments.ToList());
        }

        public async Task<IEnumerable<PaymentDetailDto>> GetAllWithDetailsAsync()
        {
            var payments = await _context.Payments
                .Include(p => p.Ride)
                    .ThenInclude(r => r.RideStops)
                .Include(p => p.Ride)
                    .ThenInclude(r => r.User)
                .Include(p => p.Ride)
                    .ThenInclude(r => r.Driver)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var paymentDetails = new List<PaymentDetailDto>();

            foreach (var payment in payments)
            {
                var pickupStop = payment.Ride?.RideStops?.OrderBy(s => s.StopOrder).FirstOrDefault();
                var dropoffStop = payment.Ride?.RideStops?.OrderByDescending(s => s.StopOrder).FirstOrDefault();

                var rideStops = payment.Ride?.RideStops?.Select(rs => new RideStopDto
                {
                    RideStopId = rs.RideStopId,
                    RideId = rs.RideId ?? Guid.Empty,
                    Latitude = rs.Latitude ?? 0,
                    Location = rs.Location ?? string.Empty,
                    Longitude = rs.Longitude ?? 0,
                    StopOrder = rs.StopOrder ?? 0
                }).ToList();

                paymentDetails.Add(new PaymentDetailDto
                {
                    PaymentId = payment.PaymentId,
                    RideId = payment.RideId,
                    PaymentMethod = payment.PaymentMethod,
                    PaidAmount = payment.PaidAmount,
                    TipAmount = payment.TipAmount,
                    AdminShare = payment.AdminShare,
                    DriverShare = payment.DriverShare,
                    PaymentStatus = payment.PaymentStatus,
                    CreatedAt = payment.CreatedAt,
                    UserId = payment.UserId,
                    DriverId = payment.DriverId,
                    PaymentToken = payment.PaymentToken,

                    // User Details
                    UserFullName = payment.Ride?.User?.FullName,
                    UserEmail = payment.Ride?.User?.Email,
                    UserPhoneNumber = payment.Ride?.User?.PhoneNumber,

                    // Driver Details
                    DriverFullName = payment.Ride?.Driver?.FullName,
                    DriverEmail = payment.Ride?.Driver?.Email,
                    DriverPhoneNumber = payment.Ride?.Driver?.PhoneNumber,
                    DriverLicensePlate = payment.Ride?.Driver?.CarLicensePlate,
                    DriverVehicleName = payment.Ride?.Driver?.VehicleName,
                    DriverVehicleColor = payment.Ride?.Driver?.VehicleColor,

                    // Ride Details
                    RideType = payment.Ride?.RideType,
                    RideFareFinal = payment.Ride?.FareFinal,
                    RideStatus = payment.Ride?.Status,
                    RideDistance = payment.Ride?.Distance,
                    RidePassengerCount = payment.Ride?.PassengerCount,
                    RideCreatedAt = payment.Ride?.CreatedAt,
                    RideStartTime = payment.Ride?.RideStartTime,
                    RideEndTime = payment.Ride?.RideEndTime,
                    PickupLocation = pickupStop?.Location,
                    DropoffLocation = dropoffStop?.Location,
                    RideStops = rideStops
                });
            }

            return paymentDetails;
        }

        public async Task<PaymentPagedResultDto> GetPagedPaymentsWithDetailsAsync(PaymentFilterDto filter)
        {
            var now = DateTime.UtcNow;

            // Get all payments with includes for filtering
            var allPaymentsQuery = _context.Payments
                .Include(p => p.Ride)
                    .ThenInclude(r => r.RideStops)
                .Include(p => p.Ride)
                    .ThenInclude(r => r.User)
                .Include(p => p.Ride)
                    .ThenInclude(r => r.Driver)
                .AsQueryable();

            // Apply date filter
            if (!string.IsNullOrEmpty(filter.FilterPeriod) && filter.FilterPeriod != "all")
            {
                DateTime startDate;

                switch (filter.FilterPeriod.ToLower())
                {
                    case "daily":
                        startDate = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
                        break;
                    case "weekly":
                        var dayOfWeek = (int)now.DayOfWeek;
                        startDate = now.Date.AddDays(-dayOfWeek);
                        startDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0, DateTimeKind.Utc);
                        break;
                    case "monthly":
                        startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                        break;
                    default:
                        startDate = DateTime.MinValue;
                        break;
                }

                allPaymentsQuery = allPaymentsQuery.Where(p => p.CreatedAt >= startDate);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                allPaymentsQuery = allPaymentsQuery.Where(p =>
                    (p.PaymentMethod != null && p.PaymentMethod.ToLower().Contains(searchTerm)) ||
                    (p.PaymentStatus != null && p.PaymentStatus.ToLower().Contains(searchTerm)) ||
                    (p.Ride != null && p.Ride.User != null && p.Ride.User.FullName != null && p.Ride.User.FullName.ToLower().Contains(searchTerm)) ||
                    (p.Ride != null && p.Ride.User != null && p.Ride.User.Email != null && p.Ride.User.Email.ToLower().Contains(searchTerm)) ||
                    (p.Ride != null && p.Ride.Driver != null && p.Ride.Driver.FullName != null && p.Ride.Driver.FullName.ToLower().Contains(searchTerm)) ||
                    (p.Ride != null && p.Ride.Driver != null && p.Ride.Driver.Email != null && p.Ride.Driver.Email.ToLower().Contains(searchTerm)) ||
                    (p.Ride != null && p.Ride.RideStops != null && p.Ride.RideStops.Any(rs => rs.Location != null && rs.Location.ToLower().Contains(searchTerm)))
                );
            }

            // Group by RideId and get the latest payment for each ride
            var latestPayments = await allPaymentsQuery
                .GroupBy(p => p.RideId)
                .Select(g => g.OrderByDescending(p => p.CreatedAt).First())
                .ToListAsync();

            // Get the payment IDs for the final query
            var latestPaymentIds = latestPayments.Select(p => p.PaymentId).ToList();

            // Now get the full details for these payment IDs
            var query = _context.Payments
                .Include(p => p.Ride)
                    .ThenInclude(r => r.RideStops)
                .Include(p => p.Ride)
                    .ThenInclude(r => r.User)
                .Include(p => p.Ride)
                    .ThenInclude(r => r.Driver)
                .Where(p => latestPaymentIds.Contains(p.PaymentId))
                .AsQueryable();

            // Apply sorting on the results
            if (!string.IsNullOrWhiteSpace(filter.SortColumn))
            {
                var isAscending = filter.SortDirection?.ToLower() != "desc";
                query = filter.SortColumn.ToLower() switch
                {
                    "createdat" => isAscending ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
                    "paidamount" => isAscending ? query.OrderBy(p => p.PaidAmount) : query.OrderByDescending(p => p.PaidAmount),
                    "tipamount" => isAscending ? query.OrderBy(p => p.TipAmount) : query.OrderByDescending(p => p.TipAmount),
                    "adminshare" => isAscending ? query.OrderBy(p => p.AdminShare) : query.OrderByDescending(p => p.AdminShare),
                    "drivershare" => isAscending ? query.OrderBy(p => p.DriverShare) : query.OrderByDescending(p => p.DriverShare),
                    "paymentstatus" => isAscending ? query.OrderBy(p => p.PaymentStatus) : query.OrderByDescending(p => p.PaymentStatus),
                    _ => query.OrderByDescending(p => p.CreatedAt)
                };
            }
            else
            {
                query = query.OrderByDescending(p => p.CreatedAt);
            }

            // Get total count of unique rides
            var totalCount = latestPaymentIds.Count;

            // Calculate earnings for the current filter (all unique rides matching the filter)
            var allFilteredPayments = await query.ToListAsync();
            var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            var weekStart = todayStart.AddDays(-(int)now.DayOfWeek);
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // Group the filtered payments by RideId again to ensure uniqueness for earnings calculation
            var uniqueFilteredRides = allFilteredPayments
                .GroupBy(p => p.RideId)
                .Select(g => g.OrderByDescending(p => p.CreatedAt).First())
                .ToList();

            var completedFilteredRides = uniqueFilteredRides
                .Where(p => p.PaymentStatus != null && p.PaymentStatus.ToLower() == "completed")
                .ToList();

            var totalEarnings = completedFilteredRides.Sum(p => p.PaidAmount ?? 0);
            var dailyEarnings = completedFilteredRides.Where(p => p.CreatedAt >= todayStart).Sum(p => p.PaidAmount ?? 0);
            var weeklyEarnings = completedFilteredRides.Where(p => p.CreatedAt >= weekStart).Sum(p => p.PaidAmount ?? 0);
            var monthlyEarnings = completedFilteredRides.Where(p => p.CreatedAt >= monthStart).Sum(p => p.PaidAmount ?? 0);

            var uniqueRidesCount = uniqueFilteredRides.Count;

            // Apply pagination
            var skip = (filter.PageNumber - 1) * filter.PageSize;
            var payments = allFilteredPayments
                .Skip(skip)
                .Take(filter.PageSize)
                .ToList();

            // Map to DTOs
            var paymentDetails = new List<PaymentDetailDto>();

            foreach (var payment in payments)
            {
                var pickupStop = payment.Ride?.RideStops?.OrderBy(s => s.StopOrder).FirstOrDefault();
                var dropoffStop = payment.Ride?.RideStops?.OrderByDescending(s => s.StopOrder).FirstOrDefault();

                var rideStops = payment.Ride?.RideStops?.Select(rs => new RideStopDto
                {
                    RideStopId = rs.RideStopId,
                    RideId = rs.RideId ?? Guid.Empty,
                    Latitude = rs.Latitude ?? 0,
                    Location = rs.Location ?? string.Empty,
                    Longitude = rs.Longitude ?? 0,
                    StopOrder = rs.StopOrder ?? 0
                }).ToList();

                paymentDetails.Add(new PaymentDetailDto
                {
                    PaymentId = payment.PaymentId,
                    RideId = payment.RideId,
                    PaymentMethod = payment.PaymentMethod,
                    PaidAmount = payment.PaidAmount,
                    TipAmount = payment.TipAmount,
                    AdminShare = payment.AdminShare,
                    DriverShare = payment.DriverShare,
                    PaymentStatus = payment.PaymentStatus,
                    CreatedAt = payment.CreatedAt,
                    UserId = payment.UserId,
                    DriverId = payment.DriverId,
                    PaymentToken = payment.PaymentToken,

                    // User Details
                    UserFullName = payment.Ride?.User?.FullName,
                    UserEmail = payment.Ride?.User?.Email,
                    UserPhoneNumber = payment.Ride?.User?.PhoneNumber,

                    // Driver Details
                    DriverFullName = payment.Ride?.Driver?.FullName,
                    DriverEmail = payment.Ride?.Driver?.Email,
                    DriverPhoneNumber = payment.Ride?.Driver?.PhoneNumber,
                    DriverLicensePlate = payment.Ride?.Driver?.CarLicensePlate,
                    DriverVehicleName = payment.Ride?.Driver?.VehicleName,
                    DriverVehicleColor = payment.Ride?.Driver?.VehicleColor,

                    // Ride Details
                    RideType = payment.Ride?.RideType,
                    RideFareFinal = payment.Ride?.FareFinal,
                    RideStatus = payment.Ride?.Status,
                    RideDistance = payment.Ride?.Distance,
                    RidePassengerCount = payment.Ride?.PassengerCount,
                    RideCreatedAt = payment.Ride?.CreatedAt,
                    RideStartTime = payment.Ride?.RideStartTime,
                    RideEndTime = payment.Ride?.RideEndTime,
                    PickupLocation = pickupStop?.Location,
                    DropoffLocation = dropoffStop?.Location,
                    RideStops = rideStops
                });
            }

            var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

            return new PaymentPagedResultDto
            {
                Payments = paymentDetails,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = totalPages,
                HasPreviousPage = filter.PageNumber > 1,
                HasNextPage = filter.PageNumber < totalPages,
                UniqueRidesCount = uniqueRidesCount,
                TotalEarnings = totalEarnings,
                DailyEarnings = dailyEarnings,
                WeeklyEarnings = weeklyEarnings,
                MonthlyEarnings = monthlyEarnings
            };
        }

        public async Task<PaymentEarningsSummaryDto> GetEarningsSummaryAsync()
        {
            var now = DateTime.UtcNow;
            var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            var weekStart = todayStart.AddDays(-(int)now.DayOfWeek);
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var allPayments = await _context.Payments.ToListAsync();

            // Group payments by RideId to get unique rides (latest payment per ride)
            var allRides = allPayments
                .GroupBy(p => p.RideId)
                .Select(g => g.OrderByDescending(p => p.CreatedAt).First())
                .ToList();

            var completedPayments = allPayments.Where(p => p.PaymentStatus != null && p.PaymentStatus.ToLower() == "completed").ToList();
            var completedRides = completedPayments
                .GroupBy(p => p.RideId)
                .Select(g => g.OrderByDescending(p => p.CreatedAt).First())
                .ToList();

            var totalEarnings = completedRides.Sum(p => p.PaidAmount ?? 0);
            var dailyEarnings = completedRides.Where(p => p.CreatedAt >= todayStart).Sum(p => p.PaidAmount ?? 0);
            var weeklyEarnings = completedRides.Where(p => p.CreatedAt >= weekStart).Sum(p => p.PaidAmount ?? 0);
            var monthlyEarnings = completedRides.Where(p => p.CreatedAt >= monthStart).Sum(p => p.PaidAmount ?? 0);

            var totalAdminShare = allRides.Sum(p => p.AdminShare ?? 0);
            var totalDriverShare = allRides.Sum(p => p.DriverShare ?? 0);
            var totalCompletedDriverShare = completedRides.Sum(p => p.DriverShare ?? 0);
            var totalTips = allRides.Sum(p => p.TipAmount ?? 0);

            var totalPayments = allRides.Count; // Now counting unique rides
            var totalCompletedPayments = completedRides.Count; // Now counting unique completed rides
            var dailyPayments = completedRides.Count(p => p.CreatedAt >= todayStart);
            var weeklyPayments = completedRides.Count(p => p.CreatedAt >= weekStart);
            var monthlyPayments = completedRides.Count(p => p.CreatedAt >= monthStart);

            return new PaymentEarningsSummaryDto
            {
                TotalEarnings = totalEarnings,
                DailyEarnings = dailyEarnings,
                WeeklyEarnings = weeklyEarnings,
                MonthlyEarnings = monthlyEarnings,
                TotalAdminShare = totalAdminShare,
                TotalDriverShare = totalDriverShare,
                TotalCompletedDriverShare = totalCompletedDriverShare,
                TotalTips = totalTips,
                TotalPayments = totalPayments,
                TotalCompletedPayments = totalCompletedPayments,
                DailyPayments = dailyPayments,
                WeeklyPayments = weeklyPayments,
                MonthlyPayments = monthlyPayments
            };
        }

        public Task<PaymentDto> GetByRideIdAsync(Guid rideId)
        {
            var payment = _context.Payments
                .Where(p => p.RideId == rideId)
                .Select(p => new PaymentDto
                {
                    PaymentId = p.PaymentId,
                    RideId = p.RideId,
                    AdminShare = p.AdminShare,
                    DriverShare = p.DriverShare,
                    PaidAmount = p.PaidAmount,
                    TipAmount = p.TipAmount,
                    PaymentMethod = p.PaymentMethod,
                    PaymentStatus = p.PaymentStatus,
                    CreatedAt = p.CreatedAt,
                    UserId = p.UserId,
                    DriverId = p.DriverId,
                    PaymentToken = p.PaymentToken,
                    TransferStatus = p.TransferStatus,
                    TransferId = p.TransferId,
                    TransferredAt = p.TransferredAt,
                    TotalAmount = (p.DriverShare ?? 0) + (p.TipAmount ?? 0)
                })
                .FirstOrDefault();
            if (payment == null)
            {
                return Task.FromResult<PaymentDto>(null);
            }
            else
            {
                return Task.FromResult(payment);
            }
        }

        public async Task UpdateAsync(PaymentDto payment)
        {
            var paymentEntity = await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == payment.PaymentId);
            if (paymentEntity != null)
            {
                // Update all fields including decimal amounts
                paymentEntity.PaidAmount = payment.PaidAmount;
                paymentEntity.TipAmount = payment.TipAmount;
                paymentEntity.AdminShare = payment.AdminShare;
                paymentEntity.DriverShare = payment.DriverShare;
                paymentEntity.PaymentMethod = payment.PaymentMethod;
                paymentEntity.PaymentStatus = string.IsNullOrWhiteSpace(payment.PaymentStatus) ? paymentEntity.PaymentStatus : payment.PaymentStatus;
                paymentEntity.UserId = payment.UserId;
                paymentEntity.DriverId = payment.DriverId;
                paymentEntity.RideId = payment.RideId;
                paymentEntity.PaymentToken = payment.PaymentToken;
                paymentEntity.TransferStatus = payment.TransferStatus;
                paymentEntity.TransferId = payment.TransferId;
                paymentEntity.TransferredAt = payment.TransferredAt;

                // Update the Ride's FareFinal to match the payment amount if RideId is provided
                if (payment.RideId.HasValue && payment.PaidAmount.HasValue)
                {
                    var ride = await _context.Rides.FirstOrDefaultAsync(r => r.RideId == payment.RideId.Value);
                    if (ride != null)
                    {
                        ride.FareFinal = payment.PaidAmount.Value;
                        _context.Rides.Update(ride);
                    }
                }

                _context.Payments.Update(paymentEntity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateTransferStatusAsync(Guid rideId, string transferStatus, string transferId, DateTime? transferredAt)
        {
            var paymentEntity = await _context.Payments
                .FirstOrDefaultAsync(p => p.RideId == rideId);

            if (paymentEntity != null)
            {
                paymentEntity.TransferStatus = transferStatus;
                paymentEntity.TransferId = transferId;
                paymentEntity.TransferredAt = transferredAt;
                paymentEntity.PaymentStatus = "completed";

                _context.Payments.Update(paymentEntity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<PaymentDto> CompleteTransactionAsync(CompleteTransactionRequest request)
        {
            // First check for existing pending payments for this rideId and get AdminShare/DriverShare
            decimal? adminShare = request.AdminShare;
            decimal? driverShare = request.DriverShare;

            // Check for existing pending payments if AdminShare/DriverShare not provided
            if (!adminShare.HasValue || !driverShare.HasValue || adminShare == 0 || driverShare == 0)
            {
                var pendingPayment = await _context.Payments
                    .Where(p => p.RideId == request.RideId && p.PaymentStatus != null && p.PaymentStatus.ToLower() == "pending")
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();

                if (pendingPayment != null)
                {
                    adminShare = pendingPayment.AdminShare;
                    driverShare = pendingPayment.DriverShare;
                }
            }

            // Only calculate if still not available from pending payments or frontend
            if (!adminShare.HasValue || !driverShare.HasValue || adminShare == 0 || driverShare == 0)
            {
                var ride = await _context.Rides
                    .Include(r => r.RideStops)
                    .FirstOrDefaultAsync(r => r.RideId == request.RideId);

                if (ride != null && ride.FareFinal.HasValue && ride.FareFinal.Value > 0)
                {
                    var pickupStop = ride.RideStops?.OrderBy(s => s.StopOrder).FirstOrDefault();
                    if (pickupStop != null)
                    {
                        string pickupLocation = pickupStop.Location ?? "";
                        var allFareSettings = await _unitOfWork.FareSettingRepository.GetAllFareSettingsAsync();
                        var fareSettings = allFareSettings
                            .FirstOrDefault(f => !string.IsNullOrEmpty(f.AreaType) &&
                                                 pickupLocation.Contains(f.AreaType, StringComparison.OrdinalIgnoreCase));

                        if (fareSettings != null && fareSettings.AdminPercentage.HasValue)
                        {
                            decimal finalFare = ride.FareFinal.Value;
                            decimal adminCommissionPercent = fareSettings.AdminPercentage.Value;
                            decimal adminCommission = finalFare * adminCommissionPercent / 100m;
                            decimal driverPayment = finalFare - adminCommission;

                            // Round to whole numbers
                            adminCommission = Math.Round(adminCommission, 0, MidpointRounding.AwayFromZero);
                            driverPayment = Math.Round(driverPayment, 0, MidpointRounding.AwayFromZero);

                            adminShare = adminCommission;
                            driverShare = driverPayment;
                        }
                        else
                        {
                            // Fallback: Use first available fare setting's admin percentage
                            var defaultFareSetting = allFareSettings
                                .FirstOrDefault(f => f.AdminPercentage.HasValue && f.AdminPercentage.Value > 0);
                            
                            if (defaultFareSetting != null && defaultFareSetting.AdminPercentage.HasValue)
                            {
                                decimal finalFare = ride.FareFinal.Value;
                                decimal adminCommissionPercent = defaultFareSetting.AdminPercentage.Value;
                                decimal adminCommission = finalFare * adminCommissionPercent / 100m;
                                decimal driverPayment = finalFare - adminCommission;

                                adminCommission = Math.Round(adminCommission, 0, MidpointRounding.AwayFromZero);
                                driverPayment = Math.Round(driverPayment, 0, MidpointRounding.AwayFromZero);

                                adminShare = adminCommission;
                                driverShare = driverPayment;
                            }
                        }
                    }

                    // Try to use AdminCommission and DriverPayment from Ride table if available
                    if (!adminShare.HasValue && !string.IsNullOrEmpty(ride.AdminCommission) && 
                        decimal.TryParse(ride.AdminCommission, out decimal rideAdminCommission) &&
                        rideAdminCommission > 0 &&
                        !string.IsNullOrEmpty(ride.DriverPayment) &&
                        decimal.TryParse(ride.DriverPayment, out decimal rideDriverPayment) &&
                        rideDriverPayment > 0)
                    {
                        adminShare = rideAdminCommission;
                        driverShare = rideDriverPayment;
                    }
                }
            }

            // Create new payment entity
            // Tip can be supplied either directly in this request OR via the /api/tip endpoint.
            // If the capture request doesn't include TipAmount, fall back to the stored Tip record.
            decimal tipAmount = request.TipAmount ?? 0m;
            if (tipAmount <= 0m && request.RideId != Guid.Empty)
            {
                try
                {
                    var tipString = await _unitOfWork.TipRepository.GetTipbyRideId(request.RideId);
                    if (!string.IsNullOrWhiteSpace(tipString) &&
                        !string.Equals(tipString, "No Tip", StringComparison.OrdinalIgnoreCase) &&
                        decimal.TryParse(tipString, out var parsedTip) &&
                        parsedTip > 0m)
                    {
                        tipAmount = parsedTip;
                    }
                }
                catch
                {
                    // If tip lookup fails, proceed with 0 tip (payment still completes).
                }
            }

            Payment paymentEntity = new Payment
            {
                PaymentId = Guid.NewGuid(),
                RideId = request.RideId,
                UserId = request.UserId,
                DriverId = request.DriverId,
                PaidAmount = (adminShare ?? 0) + (driverShare ?? 0),
                PaymentStatus = request.Status,
                CreatedAt = request.CompletedAt,
                TransferredAt = request.CompletedAt,
                TransferStatus = request.Status == "completed_balance_paid" ? "completed" : "pending",
                AdminShare = adminShare,
                DriverShare = driverShare,
                TipAmount = tipAmount,
                PromoCode = request.PromoCode,
                PaymentMethod = request.PaymentMethod,
                PaymentToken = request.PaymentToken,
                TransferId = request.TransferId
            };

            await _context.Payments.AddAsync(paymentEntity);

            // Update the Ride's FareFinal to match the payment amount
            if (request.RideId != Guid.Empty && ((adminShare ?? 0) + (driverShare ?? 0))>0)
            {
                var ride = await _context.Rides.FirstOrDefaultAsync(r => r.RideId == request.RideId);
                if (ride != null)
                {
                    ride.FareFinal = (adminShare ?? 0) + (driverShare ?? 0);
                    _context.Rides.Update(ride);
                }
            }

            await _context.SaveChangesAsync();
            await _hubContext.Clients.Group(paymentEntity.DriverId.ToString())
             .SendAsync("NewRideAssigned", new
             {
                 // Keep compatibility with driver app which listens on NewRideAssigned
                 // for end-of-ride payment completion updates.
                 RideId = paymentEntity.RideId,
                 Status = "Payment Received",
                 Payment = "Successful",
                 // Driver's earning for the ride (excluding tip)
                 FareFinal = paymentEntity.DriverShare ?? 0m,
                 // Tip (if any)
                 Tip = paymentEntity.TipAmount ?? 0m,
                 // Total earning including tip (optional convenience for clients)
                 TotalAmount = (paymentEntity.DriverShare ?? 0m) + (paymentEntity.TipAmount ?? 0m)
             });
            return new PaymentDto
            {
                PaymentId = paymentEntity.PaymentId,
                RideId = paymentEntity.RideId,
                AdminShare = paymentEntity.AdminShare,
                DriverShare = paymentEntity.DriverShare,
                PaidAmount = paymentEntity.PaidAmount,
                TipAmount = paymentEntity.TipAmount,
                PaymentMethod = paymentEntity.PaymentMethod,
                PaymentStatus = paymentEntity.PaymentStatus,
                CreatedAt = paymentEntity.CreatedAt,
                UserId = paymentEntity.UserId,
                DriverId = paymentEntity.DriverId,
                PaymentToken = paymentEntity.PaymentToken,
                PromoCode = paymentEntity.PromoCode,
                TransferStatus = paymentEntity.TransferStatus,
                TransferId = paymentEntity.TransferId,
                TransferredAt = paymentEntity.TransferredAt,
                TotalAmount = (paymentEntity.DriverShare ?? 0) + (paymentEntity.TipAmount ?? 0)
            };
        }

        public async Task<IEnumerable<HeldPaymentDto>> GetHeldPaymentsAsync()
        {
            var heldPayments = await _context.Payments
                .Where(p => p.PaymentStatus != null && p.PaymentStatus.ToLower() == "held" &&
                           p.RideId.HasValue &&
                           p.UserId != null &&
                           !_context.Payments
                               .Any(cp => cp.RideId == p.RideId && cp.PaymentStatus != null && cp.PaymentStatus.ToLower() == "completed"))
                .Select(p => new HeldPaymentDto
                {
                    RideId = p.RideId,
                    UserId = p.UserId!.Value,
                    PaymentIntentId = p.PaymentToken,
                    HeldAmount = p.PaidAmount ?? 0,
                    Status = p.PaymentStatus ?? "held",
                    CreatedAt = p.CreatedAt ?? DateTime.UtcNow
                })
                .ToListAsync();

            return heldPayments;
        }

        public async Task<HeldPaymentDto> CreateHeldPaymentAsync(CreateHeldPaymentRequest request)
        {
            // Check for existing pending payments for this rideId and get AdminShare/DriverShare
            decimal? adminShare = null;
            decimal? driverShare = null;

            if (request.RideId.HasValue)
            {
                var pendingPayment = await _context.Payments
                    .Where(p => p.RideId == request.RideId && p.PaymentStatus != null && p.PaymentStatus.ToLower() == "pending")
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();

                if (pendingPayment != null)
                {
                    adminShare = pendingPayment.AdminShare;
                    driverShare = pendingPayment.DriverShare;
                }
            }

            var paymentEntity = new Payment
            {
                PaymentId = Guid.NewGuid(),
                RideId = request.RideId,
                UserId = request.UserId,
                DriverId = request.DriverId,
                PaidAmount = request.HeldAmount,
                PaymentStatus = "held",
                PaymentToken = request.PaymentIntentId,
                PaymentMethod = request.PaymentMethod,
                CreatedAt = DateTime.UtcNow,
                TransferStatus = "pending",
                AdminShare = adminShare,
                DriverShare = driverShare
            };

            await _context.Payments.AddAsync(paymentEntity);

            // Update the Ride's FareFinal to match the payment amount
            if (request.RideId.HasValue && request.HeldAmount > 0)
            {
                var ride = await _context.Rides.FirstOrDefaultAsync(r => r.RideId == request.RideId.Value);
                if (ride != null)
                {
                    ride.FareFinal = request.HeldAmount;
                    _context.Rides.Update(ride);
                }
            }

            await _context.SaveChangesAsync();

            return new HeldPaymentDto
            {
                RideId = paymentEntity.RideId,
                UserId = paymentEntity.UserId ?? Guid.Empty,
                PaymentIntentId = paymentEntity.PaymentToken,
                HeldAmount = paymentEntity.PaidAmount ?? 0,
                Status = paymentEntity.PaymentStatus ?? "held",
                CreatedAt = paymentEntity.CreatedAt ?? DateTime.UtcNow
            };
        }
    }
}
