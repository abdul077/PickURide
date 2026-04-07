using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using PickURide.Application.Models.AllRides;
using PickURide.Infrastructure.Hub;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;

namespace PickURide.Infrastructure.Services
{
    public class RideService : IRideService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDriverLocationService _driverLocationService;
        private readonly IRideChatCacheService _chatCacheService;
        private readonly IFareSettingRepository _fareSettingRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IHubContext<RideChatHub> _hubContext;
        private readonly IFeedbackRepository _feedbackRepository;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IStripeService _stripeService;
        private readonly ILogger<RideService> _logger;

        public RideService(IUnitOfWork unitOfWork, IDriverLocationService driverLocationService,
            IRideChatCacheService chatCacheService, IFareSettingRepository fareSettingRepository,
            IPaymentRepository paymentRepository,
            IHubContext<RideChatHub> hubContext,
            IFeedbackRepository feedbackRepository,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IStripeService stripeService,
            ILogger<RideService> logger)
        {
            _unitOfWork = unitOfWork;
            _driverLocationService = driverLocationService;
            _chatCacheService = chatCacheService;
            _fareSettingRepository = fareSettingRepository;
            _paymentRepository = paymentRepository;
            _hubContext = hubContext;
            _feedbackRepository = feedbackRepository;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _stripeService = stripeService;
            _logger = logger;
        }

        public Task<string> AssignDriverAsync(Guid rideId, Guid driverId)
        {
            return _unitOfWork.RideRepository.AssignDriverAsync(rideId, driverId);
        }

        public async Task<object> BookRideAsync(BookRideRequest request)
        {
            var pickupStop = request.Stops.OrderBy(s => s.StopOrder).FirstOrDefault();
            var dropoffStop = request.Stops.OrderByDescending(s => s.StopOrder).FirstOrDefault();
            if (pickupStop == null)
                return "Pickup location is required.";

            // Calculate total distance from all ride stops using route distance
            double totalDistance = 0.0;
            var orderedStops = request.Stops.OrderBy(s => s.StopOrder).ToList();
            for (int i = 1; i < orderedStops.Count; i++)
            {
                double routeDistance = await GetRouteDistanceInKm(
                    orderedStops[i - 1].Latitude,
                    orderedStops[i - 1].Longitude,
                    orderedStops[i].Latitude,
                    orderedStops[i].Longitude
                );
                totalDistance += routeDistance;
            }

            // Match fare settings with pickup stop location
            string pickupLocation = pickupStop.Location ?? "";
            var allFareSettings = await _unitOfWork.FareSettingRepository.GetAllFareSettingsWithSlabsAsync();
            var fareSettings = allFareSettings
                .FirstOrDefault(f => !string.IsNullOrEmpty(f.AreaType) &&
                                     pickupLocation.Contains(f.AreaType, StringComparison.OrdinalIgnoreCase));

            if (fareSettings == null)
                return $"Fare settings not found for pickup location: {pickupLocation}";

            // Calculate subtotal: BaseFare + (DistanceBandPrice) using slabs
            decimal baseFare = fareSettings.BaseFare ?? 0m;
            var distancePrice = CalculateDistancePriceFromSlabs(totalDistance, fareSettings.Slabs);
            decimal calculatedFare = baseFare + distancePrice;

            // Ensure minimum base price of 5
            const decimal minimumBaseFare = 5;
            decimal subtotal = calculatedFare < minimumBaseFare ? minimumBaseFare : calculatedFare;

            // Round to whole number
            subtotal = Math.Round(subtotal, 0, MidpointRounding.AwayFromZero);

            // Apply promo (flat discount) on subtotal (server enforced)
            var promoResult = await TryApplyPromoPreviewAsync(request.PromoCode, request.UserId, subtotal);
            var totalAfterDiscount = promoResult.Total;

            // Admin commission & driver payment
            decimal adminCommissionPercent = Convert.ToDecimal(fareSettings.AdminPercentage);
            decimal adminCommission = totalAfterDiscount * adminCommissionPercent / 100m;
            decimal driverPayment = totalAfterDiscount - adminCommission;

            // Round commission and payment to whole numbers
            adminCommission = Math.Round(adminCommission, 0, MidpointRounding.AwayFromZero);
            driverPayment = Math.Round(driverPayment, 0, MidpointRounding.AwayFromZero);

            // Create ride
            var rideId = Guid.NewGuid();
            var ride = new RideDto
            {
                RideId = rideId,
                UserId = request.UserId,
                RideType = request.RideType,
                IsScheduled = request.IsScheduled,
                ScheduledTime = request.ScheduledTime,
                PassengerCount = request.PassengerCount,
                FareEstimate = totalAfterDiscount,
                FareFinal = totalAfterDiscount,
                AdminCommission = adminCommission.ToString(),
                DriverPayment = driverPayment.ToString(),
                Status = "Waiting",
                CreatedAt = DateTime.UtcNow,
                PromoCode = promoResult.PromoCodeApplied,
                PromoDiscount = promoResult.Discount > 0 ? promoResult.Discount : null,
                RideStops = request.Stops.Select(stop => new RideStopDto
                {
                    RideStopId = Guid.NewGuid(),
                    RideId = rideId,
                    StopOrder = stop.StopOrder,
                    Location = stop.Location,
                    Latitude = stop.Latitude,
                    Longitude = stop.Longitude
                }).ToList(),
                Distance = totalDistance,
                PickupLocation = pickupStop.Location,
                DropoffLocation = dropoffStop?.Location
            };

            if (!request.IsScheduled)
            {
                // Find available drivers
                var availableDrivers = await _unitOfWork.DriverRepository.GetAllAvailableDriversAsync();
                if (!availableDrivers.Any())
                    return "No available drivers at the moment.";

                var liveLocations = await _driverLocationService.GetLiveLocationsAsync();
                var matchedDrivers = availableDrivers
                    .Join(liveLocations,
                          dbDriver => dbDriver.DriverId,
                          cache => cache.DriverId,
                          (dbDriver, cache) => new
                          {
                              Driver = dbDriver,
                              Location = cache
                          })
                    .ToList();

                if (!matchedDrivers.Any())
                    return "No live drivers available right now.";

                var nearest = matchedDrivers
                    .OrderBy(x => GetDistanceInKm(
                        pickupStop.Latitude,
                        pickupStop.Longitude,
                        x.Location.Latitude ?? 0.0,
                        x.Location.Longitude ?? 0.0
                    ))
                    .First();

                ride.DriverId = nearest.Driver.DriverId;
                ride.DriverPayment = driverPayment.ToString();
                ride.AdminCommission = adminCommission.ToString();
                ride.FareEstimate = totalAfterDiscount;
                ride.Status = "Pending";
                nearest.Driver.Status = "Assigned";

                await _unitOfWork.RideRepository.AddAsync(ride);
                await _unitOfWork.SaveAsync();

                // Consume promo usage on booking (estimate preview does not consume)
                if (!string.IsNullOrWhiteSpace(promoResult.PromoCodeApplied) && promoResult.Discount > 0)
                {
                    await _unitOfWork.PromoRepository.AddRedemptionAsync(
                        promoResult.PromoCodeId!.Value,
                        request.UserId,
                        ride.RideId,
                        promoResult.Discount);
                }
                // await CreatePrepaidPaymentAsync(ride, finalFare, adminCommission, driverPayment, request.PaymentToken, request.PaymentStatus, request.TransferStatus);

                var user = await _unitOfWork.UserRepository.SingleUser(request.UserId);

                await SendNewRideAssignedAsync(nearest.Driver.DriverId, ride.RideId, new NewRideAssignedSignalRDto
                {
                    RideId = ride.RideId,
                    RideType = ride.RideType,
                    FareEstimate = ride.DriverPayment,
                    CreatedAt = ride.CreatedAt,
                    Status = "Waiting",
                    PassengerId = user.UserId,
                    PassengerName = user.FullName,
                    PassengerPhone = user.PhoneNumber,
                    PickupLocation = pickupStop?.Location,
                    PickUpLat = pickupStop?.Latitude,
                    PickUpLon = pickupStop?.Longitude,
                    DropoffLocation = dropoffStop?.Location,
                    DropoffLat = dropoffStop?.Latitude,
                    DropoffLon = dropoffStop?.Longitude,
                    Stops = orderedStops.Select(s => new RideStopSignalRDto
                    {
                        StopOrder = s.StopOrder,
                        Location = s.Location,
                        Latitude = s.Latitude,
                        Longitude = s.Longitude
                    }).ToList(),
                    PassengerCount = ride.PassengerCount
                });

                // Get driver's average rating
                var driverAverageRating = await _feedbackRepository.GetAverageRatingByDriverAsync(nearest.Driver.DriverId);

                // Return required fields
                return new
                {
                    RideId = ride.RideId,
                    DriverId = nearest.Driver.DriverId,
                    DriverName = nearest.Driver.FullName,
                    DriverPhone = nearest.Driver.PhoneNumber,
                    DriverAverageRating = driverAverageRating,
                    DriverStripeAccount = nearest.Driver.StripeAccountId,
                    PassengerName = user.FullName,
                    PassengerPhone = user.PhoneNumber,
                    EstimatedPrice = totalAfterDiscount,
                    RideStatus = ride.Status,
                    Vehicle = nearest.Driver.VehicleName,
                    VehicleColor = nearest.Driver.VehicleColor,
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
                    }).ToList()
                };
            }
            else
            {
                ride.DriverId = null;
                await _unitOfWork.RideRepository.AddAsync(ride);
                await _unitOfWork.SaveAsync();

                if (!string.IsNullOrWhiteSpace(promoResult.PromoCodeApplied) && promoResult.Discount > 0)
                {
                    await _unitOfWork.PromoRepository.AddRedemptionAsync(
                        promoResult.PromoCodeId!.Value,
                        request.UserId,
                        ride.RideId,
                        promoResult.Discount);
                }
                // await CreatePrepaidPaymentAsync(ride, finalFare, adminCommission, driverPayment, request.PaymentToken, request.PaymentStatus, request.TransferStatus);

                return new
                {
                    RideId = ride.RideId,
                    DriverId = Guid.Empty,
                    DriverName = "Not Assigned",
                    DriverPhone = "Not Assigned",
                    EstimatedPrice = totalAfterDiscount,
                    RideStatus = ride.Status,
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
                    }).ToList()
                };
            }
        }
        public async Task<object> EndRideAsync(Guid rideId)
        {
            try
            {
                var requestRideStops = await _unitOfWork.RideRepository.GetRideStops(rideId);
                var dropoffStop = requestRideStops.OrderByDescending(s => s.StopOrder).FirstOrDefault();
                var orderedStops = requestRideStops.OrderBy(s => s.StopOrder).ToList();

                var ride = await _unitOfWork.RideRepository.GetEntityByIdAsync(rideId);
                if (ride == null)
                    return "Ride not found.";

                var user = await _unitOfWork.UserRepository.SingleUser(ride.UserId);

                var pickupStop = ride.RideStops?.OrderBy(s => s.StopOrder).FirstOrDefault();
                if (pickupStop == null)
                    return "Pickup stop not found.";

                string pickupLocation = pickupStop.Location ?? "";
                var allFareSettings = await _unitOfWork.FareSettingRepository.GetAllFareSettingsWithSlabsAsync();
                var fareSettings = allFareSettings
                    .FirstOrDefault(f => !string.IsNullOrEmpty(f.AreaType) &&
                                         pickupLocation.Contains(f.AreaType, StringComparison.OrdinalIgnoreCase));

                if (fareSettings == null)
                    return $"Fare settings not found for pickup location: {pickupLocation}";

                // Parse distance from string, default to 0 if null or invalid
                double distance = 0.0;
                string? rideDistance = ride.Distance.ToString();
                if (!string.IsNullOrEmpty(rideDistance) && double.TryParse(rideDistance, out double parsedDistance))
                {
                    distance = parsedDistance;
                }

                // Calculate fare components
                decimal baseFare = fareSettings.BaseFare ?? 0m;
                decimal distancePrice = CalculateDistancePriceFromSlabs(distance, fareSettings.Slabs);

                decimal perMinute = fareSettings.PerMinuteRate ?? 0m;
                int totalWaitingMinutes = 0;
                if (ride.TotalWaitingTime is TimeOnly waitingTime)
                    totalWaitingMinutes = waitingTime.Hour * 60 + waitingTime.Minute;
                const int freeWaitingMinutes = 5;
                int billableWaitingMinutes = Math.Max(totalWaitingMinutes - freeWaitingMinutes, 0);
                decimal waitingPrice = perMinute * billableWaitingMinutes;

                // Calculate final fare: BaseFare + DistancePrice + WaitingPrice
                decimal calculatedFare = baseFare + distancePrice + waitingPrice;

                // Ensure minimum base price of 5
                const decimal minimumBaseFare = 5;
                decimal subtotalFinal = calculatedFare < minimumBaseFare ? minimumBaseFare : calculatedFare;

                // Round to whole number
                subtotalFinal = Math.Round(subtotalFinal, 0, MidpointRounding.AwayFromZero);

                // Apply previously-booked promo (do NOT redeem again)
                decimal promoDiscount = 0m;
                if (!string.IsNullOrWhiteSpace(ride.PromoCode) && ride.PromoDiscount.HasValue && ride.PromoDiscount.Value > 0)
                {
                    promoDiscount = Math.Min(ride.PromoDiscount.Value, subtotalFinal);
                    promoDiscount = Math.Round(promoDiscount, 0, MidpointRounding.AwayFromZero);
                }

                decimal finalFare = subtotalFinal - promoDiscount;
                if (finalFare < 0) finalFare = 0;

                decimal adminCommissionPercent = Convert.ToDecimal(fareSettings.AdminPercentage);
                decimal adminCommission = finalFare * adminCommissionPercent / 100m;
                decimal driverPayment = finalFare - adminCommission;

                // Round commission and payment to whole numbers
                adminCommission = Math.Round(adminCommission, 0, MidpointRounding.AwayFromZero);
                driverPayment = Math.Round(driverPayment, 0, MidpointRounding.AwayFromZero);

                ride.Status = "Completed";
                ride.FareFinal = finalFare;
                ride.PromoDiscount = promoDiscount > 0 ? promoDiscount : ride.PromoDiscount;
                ride.RideEndTime = TimeOnly.FromDateTime(DateTime.Now);
                ride.AdminCommission = adminCommission.ToString();
                ride.DriverPayment = driverPayment.ToString();

                var driver = ride.DriverId.HasValue
                    ? await _unitOfWork.DriverRepository.GetByIdAsync(ride.DriverId.Value)
                    : null;
                if (driver != null)
                {
                    driver.Status = "Available";
                }

                // Process Stripe transfer to driver
                try
                {
                    var payment = await _paymentRepository.GetByRideIdAsync(rideId);
                    if (payment != null && driver != null)
                    {
                        // Check if payment has paymentIntentId and driver has StripeAccountId
                        if (!string.IsNullOrEmpty(payment.PaymentToken) &&
                            !string.IsNullOrEmpty(driver.StripeAccountId) &&
                            payment.TransferStatus != "completed")
                        {
                            // Get tip amount if exists
                            var tipString = await _unitOfWork.TipRepository.GetTipbyRideId(rideId);
                            decimal tipAmount = 0m;
                            if (!string.IsNullOrEmpty(tipString) && tipString != "No Tip" && decimal.TryParse(tipString, out decimal parsedTip))
                            {
                                tipAmount = parsedTip;
                            }
                            decimal totalAmount = finalFare + tipAmount;

                            _logger.LogInformation("Initiating Stripe transfer for ride {RideId}. PaymentIntentId: {PaymentIntentId}, DriverAccountId: {DriverAccountId}",
                                rideId, payment.PaymentToken, driver.StripeAccountId);

                            // Transfer payment to driver using Stripe
                            var transferResult = await _stripeService.TransferToDriverAsync(
                                payment.PaymentToken, // paymentIntentId
                                driver.StripeAccountId,
                                totalAmount,
                                adminCommission,
                                driverPayment,
                                rideId
                            );

                            if (transferResult.Success)
                            {
                                // Update payment record with transfer details
                                await _paymentRepository.UpdateTransferStatusAsync(
                                    rideId,
                                    "completed",
                                    transferResult.TransferId,
                                    DateTime.UtcNow
                                );

                                _logger.LogInformation("Stripe transfer completed successfully for ride {RideId}. TransferId: {TransferId}",
                                    rideId, transferResult.TransferId);
                            }
                            else
                            {
                                _logger.LogWarning("Stripe transfer failed for ride {RideId}. Error: {ErrorMessage}",
                                    rideId, transferResult.ErrorMessage);
                                // Don't fail the ride completion, just log the error
                                // You may want to add retry logic or alert admins
                            }
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(payment.PaymentToken))
                            {
                                _logger.LogWarning("Payment Intent ID not found for ride {RideId}", rideId);
                            }
                            if (driver != null && string.IsNullOrEmpty(driver.StripeAccountId))
                            {
                                _logger.LogWarning("Driver {DriverId} does not have Stripe account configured for ride {RideId}",
                                    driver.DriverId, rideId);
                            }
                            if (payment.TransferStatus == "completed")
                            {
                                _logger.LogInformation("Transfer already completed for ride {RideId}", rideId);
                            }
                        }
                    }
                    else
                    {
                        if (payment == null)
                        {
                            _logger.LogWarning("Payment record not found for ride {RideId}", rideId);
                        }
                        if (driver == null)
                        {
                            _logger.LogWarning("Driver not found for ride {RideId}", rideId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Stripe transfer for ride {RideId}. Ride will still be marked as completed.", rideId);
                    // Continue with ride completion even if transfer fails
                }

                var chatDtos = _chatCacheService.GetMessages(rideId);
                if (chatDtos.Any())
                {
                    var saveDtos = chatDtos.Select(m => new SaveRideMessageDto
                    {
                        RideId = m.RideId,
                        SenderId = m.SenderId,
                        SenderRole = m.SenderRole,
                        Message = m.Message,
                        SentAt = m.SentAt
                    }).ToList();

                    await _unitOfWork.RideMessageRepository.AddRangeAsync(saveDtos);
                    _chatCacheService.ClearMessages(rideId);
                }
                await _unitOfWork.RideRepository.UpdateAsync(ride);

                // Send SignalR message to driver if driver exists
                if (driver != null && ride.DriverId.HasValue)
                {
                    await SendNewRideAssignedAsync(ride.DriverId.Value, ride.RideId, new NewRideAssignedSignalRDto
                    {
                        RideId = ride.RideId,
                        RideType = ride.RideType,
                        FareFinal = driverPayment.ToString(),
                        CreatedAt = ride.CreatedAt,
                        Status = ride.Status,
                        PassengerId = user.UserId,
                        PassengerName = user.FullName,
                        PassengerPhone = user.PhoneNumber,
                        PickupLocation = pickupStop?.Location,
                        PickUpLat = pickupStop?.Latitude,
                        PickUpLon = pickupStop?.Longitude,
                        DropoffLocation = dropoffStop?.Location,
                        DropoffLat = dropoffStop?.Latitude,
                        DropoffLon = dropoffStop?.Longitude,
                        Stops = orderedStops.Select(s => new RideStopSignalRDto
                        {
                            StopOrder = s.StopOrder,
                            Location = s.Location,
                            Latitude = s.Latitude,
                            Longitude = s.Longitude
                        }).ToList(),
                        PassengerCount = ride.PassengerCount
                    });
                }

                await _unitOfWork.SaveAsync();
               
                var tip = await _unitOfWork.TipRepository.GetTipbyRideId(rideId);
                await _hubContext.Clients.Group(rideId.ToString())
                  .SendAsync("RideStatusChanged", new
                  {
                      ride.RideId,
                      ride.Status,
                      DriverName = driver?.FullName,
                      DriverPhone = driver?.PhoneNumber,
                      ride.FareEstimate,
                      ride.TotalWaitingTime,
                      Tip=tip
                  });
                return new
                {
                    RideId = ride.RideId,
                    FinalFare = finalFare,
                    Distance = distance,
                    TotalWaitingTime = ride.TotalWaitingTime,
                    Status = ride.Status,
                    RideEndTime = ride.RideEndTime,
                    RideStartTime = ride.RideStartTime,
                    Tip = tip
                };
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public async Task<object> FareEstimate(string Address, decimal distance, string duration, Guid? userId = null, string? promoCode = null)
        {
            var allFareSettings = await _unitOfWork.FareSettingRepository.GetAllFareSettingsWithSlabsAsync();
            var fareSettings = allFareSettings
               .FirstOrDefault(f => !string.IsNullOrEmpty(f.AreaType) &&
                                    Address.Contains(f.AreaType, StringComparison.OrdinalIgnoreCase));
            if (fareSettings == null)
                return new { Error = $"Fare settings not found for pickup location: {Address}" };

            // Calculate subtotal: BaseFare + (DistanceBandPrice)
            decimal baseFare = fareSettings.BaseFare ?? 0m;
            decimal calculatedFare = baseFare + CalculateDistancePriceFromSlabs((double)distance, fareSettings.Slabs);

            // Ensure minimum base price of 5
            const decimal minimumBaseFare = 5;
            decimal subtotal = calculatedFare < minimumBaseFare ? minimumBaseFare : calculatedFare;

            // Round to whole number
            subtotal = Math.Round(subtotal, 0, MidpointRounding.AwayFromZero);

            var promoResult = await TryApplyPromoPreviewAsync(promoCode, userId, subtotal);

            return new
            {
                Subtotal = subtotal,
                Discount = promoResult.Discount,
                Total = promoResult.Total,
                Currency = "CAD",
                AdminPercentage = fareSettings.AdminPercentage,
                PromoCodeApplied = promoResult.PromoCodeApplied,
                PromoError = promoResult.Error,
                Message = "Fare is calculated by distance. It may vary if you have multiple stops and have waiting times."
            };
        }

        private sealed class PromoApplyResult
        {
            public Guid? PromoCodeId { get; set; }
            public string? PromoCodeApplied { get; set; }
            public decimal Discount { get; set; }
            public decimal Total { get; set; }
            public string? Error { get; set; }
        }

        private static string NormalizePromoCode(string? promoCode)
        {
            return (promoCode ?? string.Empty).Trim().ToUpperInvariant();
        }

        private async Task<PromoApplyResult> TryApplyPromoPreviewAsync(string? promoCode, Guid? userId, decimal subtotal)
        {
            var normalized = NormalizePromoCode(promoCode);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return new PromoApplyResult { Discount = 0m, Total = subtotal };
            }

            var promo = await _unitOfWork.PromoRepository.GetByCodeAsync(normalized);
            if (promo == null)
            {
                return new PromoApplyResult { Discount = 0m, Total = subtotal, Error = "Invalid promo code." };
            }

            if (!promo.IsActive)
            {
                return new PromoApplyResult { PromoCodeId = promo.PromoCodeId, PromoCodeApplied = normalized, Discount = 0m, Total = subtotal, Error = "Promo code is inactive." };
            }

            if (promo.ExpiryUtc.HasValue && promo.ExpiryUtc.Value <= DateTime.UtcNow)
            {
                return new PromoApplyResult { PromoCodeId = promo.PromoCodeId, PromoCodeApplied = normalized, Discount = 0m, Total = subtotal, Error = "Promo code is expired." };
            }

            if (promo.MinFare.HasValue && subtotal < promo.MinFare.Value)
            {
                return new PromoApplyResult { PromoCodeId = promo.PromoCodeId, PromoCodeApplied = normalized, Discount = 0m, Total = subtotal, Error = $"Minimum fare required: {promo.MinFare.Value:0.00}." };
            }

            // Per-user usage limit validation (preview only; redemption happens on booking)
            if (userId.HasValue)
            {
                var count = await _unitOfWork.PromoRepository.GetUserRedemptionCountAsync(promo.PromoCodeId, userId.Value);
                if (count >= promo.PerUserLimit)
                {
                    return new PromoApplyResult { PromoCodeId = promo.PromoCodeId, PromoCodeApplied = normalized, Discount = 0m, Total = subtotal, Error = "Promo code already used." };
                }
            }

            var discount = Math.Min(promo.FlatAmount, subtotal);
            discount = Math.Round(discount, 0, MidpointRounding.AwayFromZero);

            var total = subtotal - discount;
            if (total < 0) total = 0;

            return new PromoApplyResult
            {
                PromoCodeId = promo.PromoCodeId,
                PromoCodeApplied = normalized,
                Discount = discount,
                Total = total
            };
        }

        private static decimal CalculateDistancePriceFromSlabs(double totalDistanceKm, List<PickURide.Application.Models.FareDistanceSlabDto>? slabs)
        {
            if (totalDistanceKm <= 0)
                return 0m;

            var ordered = (slabs ?? new List<PickURide.Application.Models.FareDistanceSlabDto>())
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.FromKm)
                .ToList();

            // Back-compat: if no slabs present, charge nothing beyond base fare.
            if (ordered.Count == 0)
                return 0m;

            decimal total = 0m;
            decimal distance = (decimal)totalDistanceKm;

            foreach (var slab in ordered)
            {
                var from = slab.FromKm;
                var to = slab.ToKm; // null => open ended
                var rate = slab.RatePerKm;

                if (distance <= from)
                    continue;

                var upper = to ?? distance;
                var bandDistance = Math.Min(distance, upper) - from;
                if (bandDistance > 0)
                {
                    total += bandDistance * rate;
                }

                if (to == null || distance <= upper)
                    break;
            }

            return total;
        }

        public async Task<PaginatedResult<AllRidesDto>> GetAllRidesAsync(RidePaginationRequest request)
        {
            var rides = await _unitOfWork.RideRepository.GetAllRidesAsync(request);
            return rides;
        }

        public async Task<Dictionary<string, int>> GetRideStatusCountsAsync(string? filterPeriod, bool? isScheduledFilter = null)
        {
            return await _unitOfWork.RideRepository.GetRideStatusCountsAsync(filterPeriod, isScheduledFilter);
            //    return rides.Select(r => new AllRidesDto
            //    {
            //        RideId = r.RideId,
            //        UserId = r.UserId,
            //        DriverId = r.DriverId,
            //        RideType = r.RideType,
            //        IsScheduled = r.IsScheduled,
            //        ScheduledTime = r.ScheduledTime,
            //        PassengerCount = r.PassengerCount,
            //        FareEstimate = r.FareEstimate,
            //        FareFinal = r.FareFinal,
            //        Status = r.Status,
            //        CreatedAt = r.CreatedAt,
            //        UserName = r.UserName,
            //        DriverName = r.DriverName,
            //        Feedbacks = r.Feedbacks.Select(f => new RideFeedbackDto
            //        {
            //            FeedbackId = f.FeedbackId,
            //            Comment = f.Comment,
            //            Rating = Convert.ToInt32(f.Rating),
            //        }).ToList(),
            //        Payments = r.Payments.Select(p => new RidePaymentDto
            //        {
            //            PaymentId = p.PaymentId,
            //            Amount = p.Amount,
            //            Method = p.Method
            //        }).ToList(),
            //        RideMessages = r.RideMessages.Select(m => new RideMessageDto
            //        {
            //            MessageId = m.MessageId,
            //            SenderId = m.SenderId,
            //            SenderName = m.SenderName = m.SenderRole == "User"
            //? r.UserName
            //: m.SenderRole == "Driver"
            //    ? r.DriverName
            //    : null,
            //            SenderRole = m.SenderRole,
            //            Message = m.Message,
            //            SentAt = m.SentAt
            //        }).ToList(),
            //        RideStops = r.RideStops.Select(s => new RideStopsDto
            //        {
            //            RideStopId = s.RideStopId,
            //            StopOrder = s.StopOrder,
            //            Location = s.Location,
            //            Latitude = s.Latitude,
            //            Longitude = s.Longitude
            //        }).ToList(),
            //        Tips = r.Tips.Select(t => new RideTipDto
            //        {
            //            TipId = t.TipId,
            //            Amount = t.Amount
            //        }).ToList()
            //    }).ToList();
        }

        public Task<object> GetDriverLastRide(Guid driverId)
        {
            return _unitOfWork.RideRepository.GetDriverLastRide(driverId);
        }

        public Task<object> GetUserLastRide(Guid userId)
        {
            return _unitOfWork.RideRepository.GetUserLastRide(userId);
        }

        public async Task<string> CancelRideAsync(Guid rideId)
        {
            var ride = await _unitOfWork.RideRepository.GetEntityByIdAsync(rideId);
            if (ride == null)
                return "Ride not found.";

            // Check if ride can be cancelled
            if (ride.Status == "Completed")
                return "Cannot cancel a completed ride.";

            if (ride.Status == "Cancelled")
                return "Ride is already cancelled.";

            var payment = _paymentRepository.GetByRideIdAsync(rideId);
            await CreatePrepaidPaymentAsync(ride, Convert.ToDecimal(payment.Result.PaidAmount), Convert.ToDecimal(payment.Result.AdminShare), Convert.ToDecimal(payment.Result.DriverShare), payment.Result.PaymentToken, "cancelled", "cancelled");

            // Cancel the ride
            var result = await _unitOfWork.RideRepository.CancelRideAsync(rideId);

            // If driver was assigned, notify via SignalR
            if (ride.DriverId.HasValue)
            {
                await _hubContext.Clients.Group(ride.DriverId.Value.ToString())
                    .SendAsync("RideCancelled", new
                    {
                        RideId = ride.RideId,
                        Status = "Cancelled"
                    });
            }

            // Notify user via SignalR
            await _hubContext.Clients.Group(rideId.ToString())
                .SendAsync("RideStatusChanged", new
                {
                    RideId = ride.RideId,
                    Status = "Cancelled"
                });

            return result;
        }

        public async Task<RideDto?> GetRideByIdAsync(Guid rideId)
        {
            return await _unitOfWork.RideRepository.GetByIdAsync(rideId);
        }

        public Task<List<RideDto>> GetRidesHistory(Guid driverId)
        {
            return _unitOfWork.RideRepository.GetRidesHistory(driverId);
        }

        public Task<List<RideDto>> GetScheduleRidesHistory(Guid driverId)
        {
            return _unitOfWork.RideRepository.GetScheduleRidesHistory(driverId);
        }

        public Task<List<UserRideHistory>> GetUserCompletedRidesHistory(Guid userId)
        {
            return _unitOfWork.RideRepository.GetUserCompletedRidesHistory(userId);
        }

        public Task<RideHistoryResponse<UserRideHistory>> GetUserRidesHistory(Guid userId)
        {
            return _unitOfWork.RideRepository.GetUserRidesHistory(userId);
        }

        public Task<RideHistoryResponse<UserRideHistory>> GetUserScheduleRidesHistory(Guid userId)
        {
            return _unitOfWork.RideRepository.GetUserScheduleRidesHistory(userId);
        }

        public async Task<string> SetWaitingStatusAsync(Guid rideId)
        {
            var ride = await _unitOfWork.RideRepository.GetEntityByIdAsync(rideId);
            if (ride == null)
                return "Ride not found.";

            if (!string.Equals(ride.Status, "In-Progress", StringComparison.OrdinalIgnoreCase))
                return "Waiting status can only be added when ride status is In-Progress.";

            // Get pickup location to match fare settings
            var requestRideStops = await _unitOfWork.RideRepository.GetRideStops(rideId);
            var pickupStop = requestRideStops.OrderBy(s => s.StopOrder).FirstOrDefault();
            if (pickupStop == null)
                return "Pickup stop not found.";

            string pickupLocation = pickupStop.Location ?? "";
            var allFareSettings = await _unitOfWork.FareSettingRepository.GetAllFareSettingsAsync();
            var fareSettings = allFareSettings
                .FirstOrDefault(f => !string.IsNullOrEmpty(f.AreaType) &&
                                     pickupLocation.Contains(f.AreaType, StringComparison.OrdinalIgnoreCase));

            if (fareSettings == null)
                return $"Fare settings not found for pickup location: {pickupLocation}";

            // Calculate driver payment from current fare estimate
            decimal currentFare = ride.FareFinal ?? ride.FareEstimate;
            
            // Admin commission & driver payment
            decimal adminCommissionPercent = Convert.ToDecimal(fareSettings.AdminPercentage);
            decimal adminCommission = currentFare * adminCommissionPercent / 100m;
            decimal driverPayment = currentFare - adminCommission;

            // Round commission and payment to whole numbers
            adminCommission = Math.Round(adminCommission, 0, MidpointRounding.AwayFromZero);
            driverPayment = Math.Round(driverPayment, 0, MidpointRounding.AwayFromZero);

            await _hubContext.Clients.Group(rideId.ToString())
               .SendAsync("RideStatusChanged", new
               {
                   RideId = ride.RideId,
                   Status = "Waiting on Stop",
                   FareEstimate = ride.FareEstimate,
               });

            if (ride.DriverId.HasValue)
            {
                await SendNewRideAssignedAsync(ride.DriverId.Value, ride.RideId, new NewRideAssignedSignalRDto
                {
                    RideId = ride.RideId,
                    RideType = ride.RideType,
                    FareEstimate = driverPayment.ToString(),
                    CreatedAt = ride.CreatedAt,
                    Status = "Waiting on Stop",
                    PassengerCount = ride.PassengerCount
                });
            }

            return await _unitOfWork.RideRepository.SetWaitingStatus(rideId);
        }

        public async Task<string> SetArrivedStatusAsync(Guid rideId)
        {
            var ride = await _unitOfWork.RideRepository.GetEntityByIdAsync(rideId);
            if (ride == null)
                return "Ride not found.";

            const string arrivedStatus = "Arrived";
            // Keep local state consistent for outgoing SignalR + merge-cache.
            ride.Status = arrivedStatus;

            var user = await _unitOfWork.UserRepository.SingleUser(ride.UserId);

            var requestRideStops = await _unitOfWork.RideRepository.GetRideStops(rideId);
            var dropoffStop = requestRideStops.OrderByDescending(s => s.StopOrder).FirstOrDefault();
            var orderedStops = requestRideStops.OrderBy(s => s.StopOrder).ToList();
            var pickupStop = ride.RideStops?.OrderBy(s => s.StopOrder).FirstOrDefault();

            await _hubContext.Clients.Group(rideId.ToString())
               .SendAsync("RideStatusChanged", new
               {
                   RideId = ride.RideId,
                   Status = arrivedStatus,
                   FareEstimate = ride.FareEstimate,
               });

            if (ride.DriverId.HasValue)
            {
                await SendNewRideAssignedAsync(ride.DriverId.Value, ride.RideId, new NewRideAssignedSignalRDto
                {
                    RideId = ride.RideId,
                    RideType = ride.RideType,
                    FareFinal = ride.DriverPayment,
                    CreatedAt = ride.CreatedAt,
                    Status = arrivedStatus,
                    PassengerId = user.UserId,
                    PassengerName = user.FullName,
                    PassengerPhone = user.PhoneNumber,
                    PickupLocation = pickupStop?.Location,
                    PickUpLat = pickupStop?.Latitude,
                    PickUpLon = pickupStop?.Longitude,
                    DropoffLocation = dropoffStop?.Location,
                    DropoffLat = dropoffStop?.Latitude,
                    DropoffLon = dropoffStop?.Longitude,
                    Stops = orderedStops.Select(s => new RideStopSignalRDto
                    {
                        StopOrder = s.StopOrder,
                        Location = s.Location,
                        Latitude = s.Latitude,
                        Longitude = s.Longitude
                    }).ToList(),
                    PassengerCount = ride.PassengerCount
                });
            }


            return await _unitOfWork.RideRepository.SetArrivedStatus(rideId);
        }

        public async Task<string> SetWaitingTimeAsync(Guid rideId, TimeOnly waitingTime, string status)
        {
            var requestRideStops = await _unitOfWork.RideRepository.GetRideStops(rideId);
            var pickupStop = requestRideStops.OrderBy(s => s.StopOrder).FirstOrDefault();
            var dropoffStop = requestRideStops.OrderByDescending(s => s.StopOrder).FirstOrDefault();
            var orderedStops = requestRideStops.OrderBy(s => s.StopOrder).ToList();
            var ride = await _unitOfWork.RideRepository.GetEntityByIdAsync(rideId);
            if (ride == null)
                return "Ride not found.";
            var user = await _unitOfWork.UserRepository.SingleUser(ride.UserId);
            DriverDto? driver = null;
            if (ride.DriverId.HasValue)
            {
                driver = await _unitOfWork.DriverRepository.GetByIdAsync(ride.DriverId.Value);
                if (driver != null)
                {
                    driver.Status = "In-Ride";
                }
            }
            //if (!string.Equals(ride.Status, "In-Progress", StringComparison.OrdinalIgnoreCase))
            //    return "Waiting time can only be added when ride status is In-Progress.";

            await _hubContext.Clients.Group(rideId.ToString())
               .SendAsync("RideStatusChanged", new
               {
                   RideId = ride.RideId,
                   Status = status,
                   DriverName = driver?.FullName ?? "Unknown",
                   DriverPhone = driver?.PhoneNumber ?? "Unknown",
                   FareEstimate = ride.FareEstimate,
                   WaitingTime = waitingTime
               });
            if (ride.DriverId.HasValue)
            {
                await SendNewRideAssignedAsync(ride.DriverId.Value, ride.RideId, new NewRideAssignedSignalRDto
                {
                    RideId = ride.RideId,
                    RideType = ride.RideType,
                    // Keep driver-facing fare consistent with start-ride flow (driver share).
                    FareEstimate = ride.DriverPayment,
                    CreatedAt = ride.CreatedAt,
                    Status = status,
                    PassengerId = user.UserId,
                    PassengerName = user.FullName,
                    PassengerPhone = user.PhoneNumber,
                    PickupLocation = pickupStop?.Location,
                    PickUpLat = pickupStop?.Latitude,
                    PickUpLon = pickupStop?.Longitude,
                    DropoffLocation = dropoffStop?.Location,
                    DropoffLat = dropoffStop?.Latitude,
                    DropoffLon = dropoffStop?.Longitude,
                    Stops = orderedStops.Select(s => new RideStopSignalRDto
                    {
                        StopOrder = s.StopOrder,
                        Location = s.Location,
                        Latitude = s.Latitude,
                        Longitude = s.Longitude
                    }).ToList(),
                    PassengerCount = ride.PassengerCount
                });
            }

            return await _unitOfWork.RideRepository.SetWaitingTime(rideId, waitingTime, status);
        }

        public async Task<string> StartRideAsync(Guid rideId)
        {
            var requestRideStops = await _unitOfWork.RideRepository.GetRideStops(rideId);
            var pickupStop = requestRideStops.OrderBy(s => s.StopOrder).FirstOrDefault();
            var dropoffStop = requestRideStops.OrderByDescending(s => s.StopOrder).FirstOrDefault();
            var orderedStops = requestRideStops.OrderBy(s => s.StopOrder).ToList();

            var ride = await _unitOfWork.RideRepository.GetEntityByIdAsync(rideId);
            if (ride == null)
                return "Ride not found.";

            var user = await _unitOfWork.UserRepository.SingleUser(ride.UserId);

            //if (!string.Equals(ride.Status, "Pending", StringComparison.OrdinalIgnoreCase) ||
            //    !string.Equals(ride.Status, "Waiting", StringComparison.OrdinalIgnoreCase))
            //    return "Ride can only be started when ride status is Pending or Waiting.";

            ride.Status = "In-Progress";
            ride.FareFinal = ride.FareEstimate;
            ride.RideStartTime = TimeOnly.FromDateTime(DateTime.Now);

            // Update driver status to "In-Ride"
            DriverDto? driver = null;
            if (ride.DriverId.HasValue)
            {
                driver = await _unitOfWork.DriverRepository.GetByIdAsync(ride.DriverId.Value);
                if (driver != null)
                {
                    driver.Status = "In-Ride";
                }
            }

            await _unitOfWork.RideRepository.UpdateAsync(ride);
            await _unitOfWork.SaveAsync();

            await _hubContext.Clients.Group(rideId.ToString())
               .SendAsync("RideStatusChanged", new
               {
                   RideId = ride.RideId,
                   Status = "In-Progress",
                   DriverName = driver?.FullName ?? "Unknown",
                   DriverPhone = driver?.PhoneNumber ?? "Unknown",
                   FareEstimate = ride.FareEstimate,
                   TotalWaitingTime = ride.TotalWaitingTime
               });

            if (driver != null)
            {
                await SendNewRideAssignedAsync(driver.DriverId, ride.RideId, new NewRideAssignedSignalRDto
                {
                    RideId = ride.RideId,
                    RideType = ride.RideType,
                    FareEstimate = ride.DriverPayment,
                    CreatedAt = ride.CreatedAt,
                    Status = ride.Status,
                    PassengerId = user.UserId,
                    PassengerName = user.FullName,
                    PassengerPhone = user.PhoneNumber,
                    PickupLocation = pickupStop?.Location,
                    PickUpLat = pickupStop?.Latitude,
                    PickUpLon = pickupStop?.Longitude,
                    DropoffLocation = dropoffStop?.Location,
                    DropoffLat = dropoffStop?.Latitude,
                    DropoffLon = dropoffStop?.Longitude,
                    Stops = orderedStops.Select(s => new RideStopSignalRDto
                    {
                        StopOrder = s.StopOrder,
                        Location = s.Location,
                        Latitude = s.Latitude,
                        Longitude = s.Longitude
                    }).ToList()
                });
            }
            return "Ride started successfully.";
        }

        // --- SignalR helpers: ensure NewRideAssigned never loses data on partial updates ---
        private sealed class RideStopSignalRDto
        {
            public int? StopOrder { get; set; }
            public string? Location { get; set; }
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
        }

        private sealed class NewRideAssignedSignalRDto
        {
            public Guid RideId { get; set; }
            public string? RideType { get; set; }
            public object? FareEstimate { get; set; }
            public object? FareFinal { get; set; }
            public DateTime? CreatedAt { get; set; }
            public string? Status { get; set; }
            public Guid? PassengerId { get; set; }
            public string? PassengerName { get; set; }
            public string? PassengerPhone { get; set; }
            public string? PickupLocation { get; set; }
            public double? PickUpLat { get; set; }
            public double? PickUpLon { get; set; }
            public string? DropoffLocation { get; set; }
            public double? DropoffLat { get; set; }
            public double? DropoffLon { get; set; }
            public List<RideStopSignalRDto>? Stops { get; set; }
            public int? PassengerCount { get; set; }
        }

        private sealed class NewRideAssignedCacheEntry
        {
            public NewRideAssignedSignalRDto Payload { get; set; } = new();
            public DateTime LastUpdatedUtc { get; set; }
        }

        private static readonly ConcurrentDictionary<Guid, NewRideAssignedCacheEntry> _newRideAssignedCache = new();
        private static readonly TimeSpan _newRideAssignedCacheTtl = TimeSpan.FromHours(6);
        private static int _newRideAssignedCachePruneCounter;

        private Task SendNewRideAssignedAsync(Guid driverId, Guid rideId, NewRideAssignedSignalRDto payload)
        {
            var merged = MergeNewRideAssignedSignalRPayload(rideId, payload);
            return _hubContext.Clients.Group(driverId.ToString()).SendAsync("NewRideAssigned", merged);
        }

        /// <summary>
        /// If any field in the outgoing SignalR payload is null/empty, reuse the last non-null value
        /// we sent for this ride (prevents clients from losing data on partial updates).
        /// </summary>
        private static NewRideAssignedSignalRDto MergeNewRideAssignedSignalRPayload(Guid rideId, NewRideAssignedSignalRDto current)
        {
            var now = DateTime.UtcNow;

            NewRideAssignedSignalRDto? previous = null;
            if (_newRideAssignedCache.TryGetValue(rideId, out var cached) &&
                (now - cached.LastUpdatedUtc) <= _newRideAssignedCacheTtl)
            {
                previous = cached.Payload;
            }

            var merged = new NewRideAssignedSignalRDto
            {
                RideId = rideId,
                RideType = CoalesceString(current.RideType, previous?.RideType),
                FareEstimate = CoalesceObject(current.FareEstimate, previous?.FareEstimate),
                FareFinal = CoalesceObject(current.FareFinal, previous?.FareFinal),
                CreatedAt = CoalesceDateTime(current.CreatedAt, previous?.CreatedAt),
                Status = CoalesceString(current.Status, previous?.Status),
                PassengerId = CoalesceGuid(current.PassengerId, previous?.PassengerId),
                PassengerName = CoalesceString(current.PassengerName, previous?.PassengerName),
                PassengerPhone = CoalesceString(current.PassengerPhone, previous?.PassengerPhone),
                PickupLocation = CoalesceString(current.PickupLocation, previous?.PickupLocation),
                PickUpLat = current.PickUpLat ?? previous?.PickUpLat,
                PickUpLon = current.PickUpLon ?? previous?.PickUpLon,
                DropoffLocation = CoalesceString(current.DropoffLocation, previous?.DropoffLocation),
                DropoffLat = current.DropoffLat ?? previous?.DropoffLat,
                DropoffLon = current.DropoffLon ?? previous?.DropoffLon,
                Stops = (current.Stops != null && current.Stops.Count > 0) ? current.Stops : previous?.Stops,
                PassengerCount = current.PassengerCount ?? previous?.PassengerCount
            };

            _newRideAssignedCache[rideId] = new NewRideAssignedCacheEntry
            {
                Payload = merged,
                LastUpdatedUtc = now
            };

            PruneNewRideAssignedCacheOccasionally(now);
            return merged;
        }

        private static void PruneNewRideAssignedCacheOccasionally(DateTime nowUtc)
        {
            // Very lightweight pruning to avoid unbounded growth.
            if (Interlocked.Increment(ref _newRideAssignedCachePruneCounter) % 200 != 0)
                return;

            foreach (var kvp in _newRideAssignedCache)
            {
                if ((nowUtc - kvp.Value.LastUpdatedUtc) > _newRideAssignedCacheTtl)
                {
                    _newRideAssignedCache.TryRemove(kvp.Key, out _);
                }
            }
        }

        private static string? CoalesceString(string? current, string? previous)
            => !string.IsNullOrWhiteSpace(current) ? current : previous;

        private static DateTime? CoalesceDateTime(DateTime? current, DateTime? previous)
            => (current.HasValue && current.Value != default) ? current : previous;

        private static Guid? CoalesceGuid(Guid? current, Guid? previous)
            => (current.HasValue && current.Value != Guid.Empty) ? current : previous;

        private static object? CoalesceObject(object? current, object? previous)
        {
            if (current is null)
                return previous;

            if (current is string s && string.IsNullOrWhiteSpace(s))
                return previous;

            return current;
        }

        private async Task<double> GetRouteDistanceInKm(double? lat1, double? lon1, double? lat2, double? lon2)
        {
            if (!lat1.HasValue || !lon1.HasValue || !lat2.HasValue || !lon2.HasValue)
                return double.MaxValue;

            try
            {
                var apiKey = _configuration["GoogleMaps:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    // Fallback to straight-line distance if API key is not configured
                    return GetDistanceInKm(lat1, lon1, lat2, lon2);
                }

                var origin = $"{lat1.Value},{lon1.Value}";
                var destination = $"{lat2.Value},{lon2.Value}";
                var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={origin}&destination={destination}&key={apiKey}";

                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var directionsResponse = JsonConvert.DeserializeObject<GoogleDirectionsResponse>(content);

                    if (directionsResponse?.Status == "OK" && directionsResponse.Routes?.Count > 0)
                    {
                        var route = directionsResponse.Routes[0];
                        if (route.Legs?.Count > 0)
                        {
                            // Sum up all legs distance (in meters) and convert to km
                            double totalDistanceMeters = route.Legs.Sum(leg => leg.Distance?.Value ?? 0);
                            return totalDistanceMeters / 1000.0; // Convert meters to kilometers
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Fallback to straight-line distance if API call fails
            }

            // Fallback to straight-line distance calculation
            return GetDistanceInKm(lat1, lon1, lat2, lon2);
        }

        private double GetDistanceInKm(double? lat1, double? lon1, double? lat2, double? lon2)
        {
            if (!lat1.HasValue || !lon1.HasValue || !lat2.HasValue || !lon2.HasValue)
                return double.MaxValue;

            var R = 6371;
            var dLat = ToRadians(lat2.Value - lat1.Value);
            var dLon = ToRadians(lon2.Value - lon1.Value);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1.Value)) * Math.Cos(ToRadians(lat2.Value)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double angle) => angle * Math.PI / 180.0;

        private async Task CreatePrepaidPaymentAsync(RideDto ride, decimal finalFare, decimal adminCommission, decimal driverPayment, string? paymentToken, string? paymentStatus = null, string? transferStatus = null)
        {
            var payment = new PaymentDto
            {
                RideId = ride.RideId,
                UserId = ride.UserId,
                DriverId = ride.DriverId,
                PaidAmount = finalFare,
                AdminShare = adminCommission,
                DriverShare = driverPayment,
                PaymentMethod = "Prepaid",
                PaymentToken = paymentToken,
                PaymentStatus = string.IsNullOrWhiteSpace(paymentStatus) ? "pending" : paymentStatus,
                TransferStatus = string.IsNullOrWhiteSpace(transferStatus) ? "pending" : transferStatus,
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);
        }

    }

    // Helper classes for Google Directions API response
    public class GoogleDirectionsResponse
    {
        [JsonProperty("status")]
        public string? Status { get; set; }

        [JsonProperty("routes")]
        public List<GoogleRoute>? Routes { get; set; }
    }

    public class GoogleRoute
    {
        [JsonProperty("legs")]
        public List<GoogleLeg>? Legs { get; set; }
    }

    public class GoogleLeg
    {
        [JsonProperty("distance")]
        public GoogleDistance? Distance { get; set; }
    }

    public class GoogleDistance
    {
        [JsonProperty("value")]
        public int Value { get; set; }

        [JsonProperty("text")]
        public string? Text { get; set; }
    }
}
