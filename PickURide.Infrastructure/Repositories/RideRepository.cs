using Microsoft.EntityFrameworkCore;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Models;
using PickURide.Application.Models.AllRides;
using PickURide.Application.Models.Drivers;
using PickURide.Infrastructure.Data;
using PickURide.Infrastructure.Data.Entities;
using System;
using System.Linq;

namespace PickURide.Infrastructure.Repositories
{
    public class RideRepository : IRideRepository
    {
        private readonly PickURideDbContext _context;

        public RideRepository(PickURideDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(RideDto dto)
        {
            var ride = new Ride
            {
                RideId = dto.RideId,
                UserId = dto.UserId,
                DriverId = dto.DriverId,
                RideType = dto.RideType,
                IsScheduled = dto.IsScheduled,
                ScheduledTime = dto.ScheduledTime,
                PassengerCount = dto.PassengerCount,
                FareEstimate = dto.FareEstimate,
                FareFinal = dto.FareFinal,
                Status = dto.Status,
                CreatedAt = dto.CreatedAt,
                Distance = dto.Distance.ToString(),
                AdminCommission = dto.AdminCommission,
                DriverPayment = dto.DriverPayment,
                PromoCode = dto.PromoCode,
                PromoDiscount = dto.PromoDiscount,
                RideStops = dto.RideStops.Select(stop => new RideStop
                {
                    RideStopId = Guid.NewGuid(),
                    StopOrder = stop.StopOrder,
                    Location = stop.Location,
                    Latitude = stop.Latitude,
                    Longitude = stop.Longitude
                }).ToList()
            };

            await _context.Rides.AddAsync(ride);

            var driver = await _context.Drivers.FindAsync(dto.DriverId);
            if (driver != null)
            {
                driver.Status = "In-Ride";
                _context.Drivers.Update(driver);
            }

            await _context.SaveChangesAsync();
        }
        public async Task<RideDto?> GetEntityByIdAsync(Guid rideId)
        {
            var data = await _context.Rides
                .Include(r => r.Driver)
                .Include(r => r.RideStops)
                .FirstOrDefaultAsync(r => r.RideId == rideId);

            if (data == null)
                return null;

            RideDto dto = new RideDto
            {
                RideId = data.RideId,
                UserId = data.UserId ?? Guid.Empty,
                DriverId = data.DriverId ?? Guid.Empty,
                RideType = data.RideType ?? string.Empty,
                Status = data.Status ?? string.Empty,
                TotalWaitingTime = data.RideWaitingTotalTime,
                CreatedAt = Convert.ToDateTime(data.CreatedAt),
                Distance = Convert.ToDouble(data.Distance),
                FareEstimate = data.FareEstimate ?? 0,
                FareFinal = data.FareFinal,
                PromoCode = data.PromoCode,
                PromoDiscount = data.PromoDiscount,
                IsScheduled = data.IsScheduled ?? false,
                ScheduledTime = data.ScheduledTime,
                PassengerCount = data.PassengerCount ?? 0,
                RideStartTime = data.RideStartTime ?? TimeOnly.MinValue,
                RideEndTime = data.RideEndTime ?? TimeOnly.MinValue,
                RideStops = data.RideStops?.Select(rs => new RideStopDto
                {
                    RideStopId = rs.RideStopId,
                    RideId = rs.RideId ?? Guid.Empty,
                    Latitude = rs.Latitude ?? 0,
                    Location = rs.Location ?? string.Empty,
                    Longitude = rs.Longitude ?? 0,
                    StopOrder = rs.StopOrder ?? 0
                }).ToList() ?? new List<RideStopDto>()
            };

            return dto;
        }
        public async Task<RideDto?> GetByIdAsync(Guid rideId)
        {
            var ride = await _context.Rides
                .Include(r => r.RideStops)
                .Include(r => r.Driver)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.RideId == rideId);

            if (ride == null)
                return null;

            return new RideDto
            {
                RideId = ride.RideId,
                UserId = ride.UserId ?? Guid.Empty,
                DriverId = ride.DriverId ?? Guid.Empty,
                UserName = ride.User?.FullName ?? string.Empty,
                DriverName = ride.Driver?.FullName ?? string.Empty,
                RideType = ride.RideType,
                IsScheduled = ride.IsScheduled ?? false,
                ScheduledTime = ride.ScheduledTime,
                PassengerCount = ride.PassengerCount ?? 0,
                FareEstimate = ride.FareEstimate ?? 0,
                FareFinal = ride.FareFinal,
                PromoCode = ride.PromoCode,
                PromoDiscount = ride.PromoDiscount,
                Status = ride.Status,
                CreatedAt = ride.CreatedAt ?? DateTime.UtcNow,
                RideStops = ride.RideStops.Select(rs => new RideStopDto
                {
                    RideStopId = rs.RideStopId,
                    StopOrder = rs.StopOrder ?? 0,
                    Location = rs.Location,
                    Latitude = rs.Latitude ?? 0,
                    Longitude = rs.Longitude ?? 0
                }).ToList()

            };
        }
        public async Task<string> UpdateAsync(RideDto ride)
        {
            var rideData = await _context.Rides
                .FirstOrDefaultAsync(r => r.RideId == ride.RideId);

            if (rideData != null)
            {
                rideData.Status = ride.Status;

                if (ride.Status == "In-Progress")
                {
                    rideData.RideStartTime = TimeOnly.FromDateTime(DateTime.Now);
                }
                if (ride.Status == "Completed")
                {
                    rideData.FareFinal = ride.FareFinal;
                    rideData.AdminCommission = ride.AdminCommission;
                    rideData.DriverPayment = ride.DriverPayment;
                    rideData.RideEndTime = TimeOnly.FromDateTime(DateTime.Now);

                    // Calculate AdminShare and DriverShare from fare configuration
                    decimal adminShare = 0;
                    decimal driverShare = 0;
                    
                    if (!string.IsNullOrEmpty(ride.AdminCommission) && !string.IsNullOrEmpty(ride.DriverPayment))
                    {
                        // Use values from ride if they exist
                        if (decimal.TryParse(ride.AdminCommission, out decimal adminCommission))
                            adminShare = adminCommission;
                        if (decimal.TryParse(ride.DriverPayment, out decimal driverPayment))
                            driverShare = driverPayment;
                    }
                    
                    // If shares are not set or don't add up to fare, recalculate from fare configuration
                    if (adminShare == 0 && driverShare == 0 && ride.FareFinal.HasValue && ride.FareFinal.Value > 0)
                    {
                        var pickupStop = rideData.RideStops?.OrderBy(s => s.StopOrder).FirstOrDefault();
                        if (pickupStop != null)
                        {
                            string pickupLocation = pickupStop.Location ?? "";
                            var allFareSettings = await _context.FareSettings.ToListAsync();
                            var fareSettings = allFareSettings
                                .FirstOrDefault(f => !string.IsNullOrEmpty(f.AreaType) &&
                                                     pickupLocation.Contains(f.AreaType, StringComparison.OrdinalIgnoreCase));

                            if (fareSettings != null && fareSettings.AdminPercentage.HasValue)
                            {
                                decimal finalFare = ride.FareFinal.Value;
                                decimal adminCommissionPercent = fareSettings.AdminPercentage.Value;
                                adminShare = finalFare * adminCommissionPercent / 100m;
                                driverShare = finalFare - adminShare;

                                // Round to whole numbers
                                adminShare = Math.Round(adminShare, 0, MidpointRounding.AwayFromZero);
                                driverShare = Math.Round(driverShare, 0, MidpointRounding.AwayFromZero);
                            }
                        }
                    }

                   var payment = new Payment
                   {
                       PaymentId = Guid.NewGuid(),
                       RideId = ride.RideId,
                       PaidAmount = ride.FareFinal ?? 0,
                       PaymentMethod = "N/A",
                       AdminShare = adminShare,
                       DriverShare = driverShare,
                       PaymentStatus = "Pending",
                       CreatedAt = DateTime.UtcNow,
                       DriverId = ride.DriverId,
                       UserId = ride.UserId
                   };
                    await _context.Payments.AddAsync(payment);
                }
                _context.Rides.Update(rideData);

                var driver = await _context.Drivers
                    .FirstOrDefaultAsync(d => d.DriverId == rideData.DriverId);

                if (ride.Status == "Completed" && driver != null)
                {
                    driver.Status = "Available";
                    _context.Drivers.Update(driver);
                }

                await _context.SaveChangesAsync();
                return "Ride updated successfully";
            }
            else
            {
                return "Ride not found";
            }
        }

        public async Task<PaginatedResult<AllRidesDto>> GetAllRidesAsync(RidePaginationRequest request)
        {
            var query = _context.Rides
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.Driver)
                .AsQueryable();

            // Apply scheduled filter
            if (request.IsScheduledFilter.HasValue)
            {
                query = query.Where(r => r.IsScheduled == request.IsScheduledFilter.Value);
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(request.StatusFilter) && request.StatusFilter.ToLower() != "all")
            {
                var statusFilter = request.StatusFilter.Trim();
                // Match exact status or handle status variations
                query = query.Where(r => r.Status != null && 
                    (r.Status == statusFilter || 
                     // Handle "In-Progress" variations
                     (statusFilter == "In-Progress" && (r.Status == "In-Progress" || r.Status == "InProgress" || r.Status == "Ongoing")) ||
                     // Handle "Waiting" - only match "Waiting" status, not "Pending"
                     (statusFilter == "Waiting" && r.Status == "Waiting") ||
                     // Handle "Pending" - only match "Pending" status, not "Waiting"
                     (statusFilter == "Pending" && r.Status == "Pending") ||
                     // Handle "Completed"
                     (statusFilter == "Completed" && r.Status == "Completed")));
            }

            // Apply date period filter
            query = ApplyDatePeriodFilter(query, request.FilterPeriod);

            // Apply search filter if provided (case-insensitive partial match)
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchTerm = request.Search.Trim();
                query = query.Where(r =>
                    (r.User != null && r.User.FullName != null && EF.Functions.Like(r.User.FullName, $"%{searchTerm}%")) ||
                    (r.Driver != null && r.Driver.FullName != null && EF.Functions.Like(r.Driver.FullName, $"%{searchTerm}%")) ||
                    (r.RideType != null && EF.Functions.Like(r.RideType, $"%{searchTerm}%")) ||
                    (r.Status != null && EF.Functions.Like(r.Status, $"%{searchTerm}%")) ||
                    EF.Functions.Like(r.RideId.ToString(), $"%{searchTerm}%")
                );
            }

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(request.SortColumn))
            {
                var sortColumn = request.SortColumn.ToLower();
                var sortDirection = request.SortDirection?.ToLower() ?? "asc";

                query = sortColumn switch
                {
                    "userName" => sortDirection == "desc" 
                        ? query.OrderByDescending(r => r.User != null ? r.User.FullName : "") 
                        : query.OrderBy(r => r.User != null ? r.User.FullName : ""),
                    "drivername" => sortDirection == "desc" 
                        ? query.OrderByDescending(r => r.Driver != null ? r.Driver.FullName : "") 
                        : query.OrderBy(r => r.Driver != null ? r.Driver.FullName : ""),
                    "ridetype" => sortDirection == "desc" 
                        ? query.OrderByDescending(r => r.RideType) 
                        : query.OrderBy(r => r.RideType),
                    "isscheduled" => sortDirection == "desc" 
                        ? query.OrderByDescending(r => r.IsScheduled) 
                        : query.OrderBy(r => r.IsScheduled),
                    "scheduledtime" => sortDirection == "desc" 
                        ? query.OrderByDescending(r => r.ScheduledTime) 
                        : query.OrderBy(r => r.ScheduledTime),
                    "passengercount" => sortDirection == "desc" 
                        ? query.OrderByDescending(r => r.PassengerCount) 
                        : query.OrderBy(r => r.PassengerCount),
                    "fareestimate" => sortDirection == "desc" 
                        ? query.OrderByDescending(r => r.FareEstimate) 
                        : query.OrderBy(r => r.FareEstimate),
                    "createdat" => sortDirection == "desc" 
                        ? query.OrderByDescending(r => r.CreatedAt) 
                        : query.OrderBy(r => r.CreatedAt),
                    "status" => sortDirection == "desc" 
                        ? query.OrderByDescending(r => r.Status) 
                        : query.OrderBy(r => r.Status),
                    "rideid" => sortDirection == "desc" 
                        ? query.OrderByDescending(r => r.RideId) 
                        : query.OrderBy(r => r.RideId),
                    _ => query.OrderByDescending(r => r.CreatedAt) // default sorting
                };
            }
            else
            {
                // Default sorting by CreatedAt descending
                query = query.OrderByDescending(r => r.CreatedAt);
            }

            // Count total records (after filtering, before pagination)
            var totalCount = await query.CountAsync();

            // Apply pagination
            var rides = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(r => new AllRidesDto
                {
                    RideId = r.RideId,
                    UserId = r.UserId,
                    DriverId = r.DriverId,
                    RideType = r.RideType,
                    IsScheduled = r.IsScheduled,
                    ScheduledTime = r.ScheduledTime,
                    PassengerCount = r.PassengerCount,
                    FareEstimate = r.FareEstimate,
                    FareFinal = r.FareFinal,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    Distance = Convert.ToDouble(r.Distance),
                    RideStartTime = r.RideStartTime ?? TimeOnly.MinValue,
                    RideEndTime = r.RideEndTime ?? TimeOnly.MinValue,
                    TotalWaitingTime = r.RideWaitingTotalTime ?? TimeOnly.MinValue,

                    UserName = r.User != null ? r.User.FullName : null,
                    DriverName = r.Driver != null ? r.Driver.FullName : null,
                    VehicleColor = r.Driver != null ? r.Driver.VehicleColor : null,
                    VehicleName = r.Driver != null ? r.Driver.VehicleName : null,

                    Feedbacks = r.Feedbacks.Select(f => new RideFeedbackDto
                    {
                        FeedbackId = f.FeedbackId,
                        Comment = f.Comments,
                        Rating = (int)f.Rating
                    }).ToList(),

                    RideStops = r.RideStops.Select(s => new RideStopsDto
                    {
                        RideStopId = s.RideStopId,
                        StopOrder = s.StopOrder,
                        Location = s.Location,
                        Latitude = s.Latitude,
                        Longitude = s.Longitude
                    }).ToList(),

                    Tips = r.Tips.Select(t => new RideTipDto
                    {
                        TipId = t.TipId,
                        Amount = (int)t.Amount
                    }).ToList()
                })
                .ToListAsync();

            return new PaginatedResult<AllRidesDto>
            {
                Items = rides,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        private IQueryable<Ride> ApplyDatePeriodFilter(IQueryable<Ride> query, string? filterPeriod)
        {
            if (string.IsNullOrWhiteSpace(filterPeriod) || filterPeriod.ToLower() == "all")
            {
                return query;
            }

            var now = DateTime.UtcNow;
            DateTime startDate;

            switch (filterPeriod.ToLower())
            {
                case "daily":
                    startDate = now.Date;
                    return query.Where(r => r.CreatedAt.HasValue && r.CreatedAt.Value.Date >= startDate);
                case "weekly":
                    var daysUntilMonday = ((int)now.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                    startDate = now.AddDays(-daysUntilMonday).Date;
                    return query.Where(r => r.CreatedAt.HasValue && r.CreatedAt.Value.Date >= startDate);
                case "monthly":
                    startDate = new DateTime(now.Year, now.Month, 1);
                    return query.Where(r => r.CreatedAt.HasValue && r.CreatedAt.Value.Date >= startDate);
                default:
                    return query;
            }
        }

        public async Task<Dictionary<string, int>> GetRideStatusCountsAsync(string? filterPeriod, bool? isScheduledFilter = null)
        {
            var baseQuery = _context.Rides.AsNoTracking().AsQueryable();
            
            // Apply date period filter
            baseQuery = ApplyDatePeriodFilter(baseQuery, filterPeriod);

            // Apply scheduled filter if provided
            if (isScheduledFilter.HasValue)
            {
                baseQuery = baseQuery.Where(r => r.IsScheduled == isScheduledFilter.Value);
            }

            var allCount = await baseQuery.CountAsync();
            var waitingCount = await baseQuery.CountAsync(r => r.Status == "Waiting");
            var pendingCount = await baseQuery.CountAsync(r => r.Status == "Pending");
            var inProgressCount = await baseQuery.CountAsync(r => 
                r.Status == "In-Progress" || r.Status == "InProgress" || r.Status == "Ongoing");
            var completedCount = await baseQuery.CountAsync(r => r.Status == "Completed");

            return new Dictionary<string, int>
            {
                { "All", allCount },
                { "Waiting", waitingCount },
                { "Pending", pendingCount },
                { "In-Progress", inProgressCount },
                { "Completed", completedCount }
            };
        }

        public async Task<string> AssignDriverAsync(Guid rideId, Guid driverId)
        {
            var ride = await _context.Rides.FirstOrDefaultAsync(r => r.RideId == rideId);
            if (ride == null)
            {
                return "Ride not found";
            }

            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == driverId);
            if (driver == null)
            {
                return "Driver not found";
            }

            ride.DriverId = driverId;
            ride.Status = "Driver Assigned";
            _context.Rides.Update(ride);

            driver.Status = "In-Ride";
            _context.Drivers.Update(driver);

            await _context.SaveChangesAsync();
            return "Driver assigned successfully";
        }

        public async Task<string> SetWaitingTime(Guid rideId, TimeOnly waitingTime, string status)
        {
            var ride = await _context.Rides.FirstOrDefaultAsync(r => r.RideId == rideId);
            if (ride == null)
            {
                return "Ride not found";
            }

            int minutesToAdd = waitingTime.Hour * 60 + waitingTime.Minute;
            var waitingTimeSpan = TimeSpan.FromMinutes(minutesToAdd);

            if (ride.RideWaitingTotalTime == null || ride.RideWaitingTotalTime == TimeOnly.MinValue)
            {
                ride.RideWaitingTotalTime = new TimeOnly(waitingTimeSpan.Hours, waitingTimeSpan.Minutes, 0);
            }
            else
            {
                var existing = ride.RideWaitingTotalTime.Value;
                var total = existing.ToTimeSpan() + waitingTimeSpan;
                ride.RideWaitingTotalTime = new TimeOnly(total.Hours, total.Minutes, 0);
                
            }
            ride.Status = status;
            //_context.Rides.Update(ride);
            await _context.SaveChangesAsync();
            return "Status updated successfully";
        }

        public Task<RideDto> GetActiveRideByDriverIdAsync(Guid driverId)
        {
            RideDto? dto = null;
            var ride = _context.Rides
                .Include(r => r.RideStops)
                .FirstOrDefault(r => r.DriverId == driverId && r.Status == "In-Progress");
            if (ride != null)
            {
                dto = new RideDto
                {
                    RideId = ride.RideId,
                    UserId = ride.UserId ?? Guid.Empty,
                    DriverId = ride.DriverId ?? Guid.Empty,
                    RideType = ride.RideType ?? string.Empty,
                    Status = ride.Status ?? string.Empty,
                    CreatedAt = Convert.ToDateTime(ride.CreatedAt),
                    Distance = Convert.ToDouble(ride.Distance),
                    FareEstimate = ride.FareEstimate ?? 0,
                    FareFinal = ride.FareFinal,
                    IsScheduled = ride.IsScheduled ?? false,
                    ScheduledTime = ride.ScheduledTime,
                    PassengerCount = ride.PassengerCount ?? 0,
                    RideStartTime = ride.RideStartTime ?? TimeOnly.MinValue,
                    RideEndTime = ride.RideEndTime ?? TimeOnly.MinValue,
                    RideStops = ride.RideStops?.Select(rs => new RideStopDto
                    {
                        RideStopId = rs.RideStopId,
                        RideId = rs.RideId ?? Guid.Empty,
                        Latitude = rs.Latitude ?? 0,
                        Location = rs.Location ?? string.Empty,
                        Longitude = rs.Longitude ?? 0,
                        StopOrder = rs.StopOrder ?? 0
                    }).ToList() ?? new List<RideStopDto>()
                };
            }
            return Task.FromResult(dto!);
        }

        public Task<List<RideDto>> GetScheduleRidesHistory(Guid driverId)
        {
            var rides = _context.Rides
                .Where(r => r.DriverId == driverId && r.IsScheduled == true)
                .Include(r => r.RideStops)
                .Include(r => r.User)
                .Include(r => r.Driver)
                .ToList();
            var rideDtos = rides.Select(ride => new RideDto
            {
                RideId = ride.RideId,
                UserId = ride.UserId ?? Guid.Empty,
                DriverId = ride.DriverId ?? Guid.Empty,
                UserName = ride.User?.FullName ?? string.Empty,
                DriverName = ride.Driver?.FullName ?? string.Empty,
                Vehicle = ride.Driver?.VehicleName ?? string.Empty,
                VehicleColor = ride.Driver?.VehicleColor ?? string.Empty,
                RideType = ride.RideType ?? string.Empty,
                Status = ride.Status ?? string.Empty,
                CreatedAt = Convert.ToDateTime(ride.CreatedAt),
                Distance = Convert.ToDouble(ride.Distance),
                FareEstimate = ride.FareEstimate ?? 0,
                FareFinal = ride.FareFinal,
                IsScheduled = ride.IsScheduled ?? false,
                ScheduledTime = ride.ScheduledTime,
                PassengerCount = ride.PassengerCount ?? 0,
                RideStartTime = ride.RideStartTime ?? TimeOnly.MinValue,
                RideEndTime = ride.RideEndTime ?? TimeOnly.MinValue,
                DriverPayment=ride.DriverPayment??string.Empty,
                RideStops = ride.RideStops?.Select(rs => new RideStopDto
                {
                    RideStopId = rs.RideStopId,
                    RideId = rs.RideId ?? Guid.Empty,
                    Latitude = rs.Latitude ?? 0,
                    Location = rs.Location ?? string.Empty,
                    Longitude = rs.Longitude ?? 0,
                    StopOrder = rs.StopOrder ?? 0
                }).ToList() ?? new List<RideStopDto>()
            }).ToList();
            return Task.FromResult(rideDtos);
        }

        public Task<List<RideDto>> GetRidesHistory(Guid driverId)
        {
            var rides = _context.Rides
                .Where(r => r.DriverId == driverId && r.IsScheduled == false)
                .Include(r => r.RideStops)
                .Include(r => r.Tips)
                .ToList();
            var rideDtos = rides.Select(ride => new RideDto
            {
                RideId = ride.RideId,
                UserId = ride.UserId ?? Guid.Empty,
                DriverId = ride.DriverId ?? Guid.Empty,
                RideType = ride.RideType ?? string.Empty,
                Status = ride.Status ?? string.Empty,
                CreatedAt = Convert.ToDateTime(ride.CreatedAt),
                Distance = Convert.ToDouble(ride.Distance),
                FareEstimate = ride.FareEstimate ?? 0,
                FareFinal = ride.FareFinal,
                TipAmount = (ride.Tips?.Sum(t => t.Amount ?? 0)) ?? 0,
                IsScheduled = ride.IsScheduled ?? false,
                ScheduledTime = ride.ScheduledTime,
                PassengerCount = ride.PassengerCount ?? 0,
                RideStartTime = ride.RideStartTime ?? TimeOnly.MinValue,
                RideEndTime = ride.RideEndTime ?? TimeOnly.MinValue,
                DriverPayment=ride.DriverPayment??string.Empty,
                
                RideStops = ride.RideStops?.Select(rs => new RideStopDto
                {
                    RideStopId = rs.RideStopId,
                    RideId = rs.RideId ?? Guid.Empty,
                    Latitude = rs.Latitude ?? 0,
                    Location = rs.Location ?? string.Empty,
                    Longitude = rs.Longitude ?? 0,
                    StopOrder = rs.StopOrder ?? 0
                }).ToList() ?? new List<RideStopDto>()
            }).ToList();
            return Task.FromResult(rideDtos);
        }

        public Task<RideHistoryResponse<UserRideHistory>> GetUserScheduleRidesHistory(Guid userId)
        {
            var rides = _context.Rides
                .Where(r => r.UserId == userId && r.IsScheduled == true)
                .Include(r => r.RideStops)
                .Include(r => r.Payments)
                .Include(r => r.Driver)
                .ToList();
            int completedRidesCount = _context.Rides
               .Count(r => r.UserId == userId && r.Status == "Completed" && r.IsScheduled == true);
            var completeRides = _context.Rides.Where(r => r.UserId == userId && r.IsScheduled == true && r.Status == "Completed").ToList();
            decimal totalFare = completeRides.Sum(ride => ride.FareFinal ?? 0);

            var rideDtos = rides.Select(ride =>
            {
                var rideStops = ride.RideStops?.ToList() ?? new List<RideStop>();
                var pickupStop = rideStops.OrderBy(rs => rs.StopOrder).FirstOrDefault();
                var dropoffStop = rideStops.OrderByDescending(rs => rs.StopOrder).FirstOrDefault();

                return new UserRideHistory
                {
                    RideId = ride.RideId,
                    Status = ride.Status ?? string.Empty,
                    CreatedAt = Convert.ToDateTime(ride.CreatedAt),
                    Distance = Convert.ToDouble(ride.Distance),
                    FareFinal = ride.FareFinal,
                    ScheduledTime = ride.ScheduledTime,
                    RideStartTime = ride.RideStartTime ?? TimeOnly.MinValue,
                    RideEndTime = ride.RideEndTime ?? TimeOnly.MinValue,
                    PickupLocation = pickupStop?.Location ?? string.Empty,
                    DropoffLocation = dropoffStop?.Location ?? string.Empty,
                    PaymentIntentId=ride.Payments!=null && ride.Payments.Count>0 ? ride.Payments.First().PaymentToken : string.Empty,
                    PaymentStatus=ride.Payments!=null && ride.Payments.Count>0 ? ride.Payments.First().PaymentStatus : string.Empty,
                    DriverId = ride.DriverId,
                    DriverName = ride.Driver?.FullName ?? string.Empty,
                    DriverPhoneNumber = ride.Driver?.PhoneNumber ?? string.Empty,
                    VehicleName = ride.Driver?.VehicleName ?? string.Empty,
                    VehicleColor = ride.Driver?.VehicleColor ?? string.Empty
                };
            }).ToList();
            var response = new RideHistoryResponse<UserRideHistory>
            {
                Items = rideDtos,
                CompletedRides = completedRidesCount,
                TotalFare = totalFare
            };
            return Task.FromResult(response);
        }

        public Task<RideHistoryResponse<UserRideHistory>> GetUserRidesHistory(Guid userId)
        {
            var rides = _context.Rides
               .Where(r => r.UserId == userId && r.IsScheduled == false)
               .Include(r => r.RideStops)
               .Include(r => r.Tips)
               .ToList();

            // Count completed rides for this user
            int completedRidesCount = _context.Rides
                .Count(r => r.UserId == userId && r.Status == "Completed" && r.IsScheduled == false);

            // Calculate total fare from all rides (sum FareFinal, treat null as 0)
            decimal totalFare = rides.Sum(ride => ride.FareFinal ?? 0);

            var rideDtos = rides.Select(ride =>
            {
                var rideStops = ride.RideStops?.ToList() ?? new List<RideStop>();
                var pickupStop = rideStops.OrderBy(rs => rs.StopOrder).FirstOrDefault();
                var dropoffStop = rideStops.OrderByDescending(rs => rs.StopOrder).FirstOrDefault();
                var tips = ride.Tips?.ToList() ?? new List<Tip>();
                var tipSum = tips.Sum(t => t.Amount ?? 0);

                return new UserRideHistory
                {
                    RideId = ride.RideId,
                    Status = ride.Status ?? string.Empty,
                    CreatedAt = Convert.ToDateTime(ride.CreatedAt),
                    Distance = Convert.ToDouble(ride.Distance),
                    FareFinal = ride.FareFinal,
                    TipAmount = tipSum,
                    ScheduledTime = ride.ScheduledTime,
                    RideStartTime = ride.RideStartTime ?? TimeOnly.MinValue,
                    RideEndTime = ride.RideEndTime ?? TimeOnly.MinValue,
                    PickupLocation = pickupStop?.Location ?? string.Empty,
                    DropoffLocation = dropoffStop?.Location ?? string.Empty
                };
            }).ToList();
            var response = new RideHistoryResponse<UserRideHistory>
            {
                Items = rideDtos,
                CompletedRides = completedRidesCount,
                TotalFare = totalFare
            };
            return Task.FromResult(response);
        }

        public Task<List<UserRideHistory>> GetUserCompletedRidesHistory(Guid userId)
        {
            var rides = _context.Rides
              .Where(r => r.UserId == userId && r.Status == "Completed")
              .Include(r => r.RideStops)
              .ToList();
            var rideDtos = rides.Select(ride =>
            {
                var rideStops = ride.RideStops?.ToList() ?? new List<RideStop>();
                var pickupStop = rideStops.OrderBy(rs => rs.StopOrder).FirstOrDefault();
                var dropoffStop = rideStops.OrderByDescending(rs => rs.StopOrder).FirstOrDefault();

                return new UserRideHistory
                {
                    RideId = ride.RideId,
                    Status = ride.Status ?? string.Empty,
                    CreatedAt = Convert.ToDateTime(ride.CreatedAt),
                    Distance = Convert.ToDouble(ride.Distance),
                    FareFinal = ride.FareFinal,
                    ScheduledTime = ride.ScheduledTime,
                    RideStartTime = ride.RideStartTime ?? TimeOnly.MinValue,
                    RideEndTime = ride.RideEndTime ?? TimeOnly.MinValue,
                    PickupLocation = pickupStop?.Location ?? string.Empty,
                    DropoffLocation = dropoffStop?.Location ?? string.Empty
                };
            }).ToList();
            return Task.FromResult(rideDtos);
        }

        public Task<string> SetWaitingStatus(Guid rideId)
        {
            var ride = _context.Rides.FirstOrDefault(r => r.RideId == rideId);
            if (ride == null)
            {
                return Task.FromResult("Ride not found");
            }
            ride.Status = "Waiting";
            _context.Rides.Update(ride);
            _context.SaveChanges();
            return Task.FromResult("Ride status updated to Waiting");
        }

        public Task<string> SetArrivedStatus(Guid rideId)
        {
            var ride = _context.Rides.FirstOrDefault(r => r.RideId == rideId);
            if (ride == null)
            {
                return Task.FromResult("Ride not found");
            }
            ride.Status = "Arrived";
            _context.Rides.Update(ride);
            _context.SaveChanges();
            return Task.FromResult("Ride status updated to Arrived");
        }

        public async Task<List<RideStopDto>> GetRideStops(Guid rideId)
        {
            var rideStops=await _context.RideStops.Where(m=>m.RideId==rideId).ToListAsync();
            var rideStopDtos = rideStops.Select(rs => new RideStopDto
            {
                RideStopId = rs.RideStopId,
                RideId = rs.RideId ?? Guid.Empty,
                Latitude = rs.Latitude ?? 0,
                Location = rs.Location ?? string.Empty,
                Longitude = rs.Longitude ?? 0,
                StopOrder = rs.StopOrder ?? 0
            }).ToList();
            return rideStopDtos;
        }

        public async Task<object> GetDriverLastRide(Guid driverId)
        {
            var ride = await _context.Rides
                 .Where(r => r.DriverId == driverId)
                 .Include(r => r.RideStops)
                 .Include(r => r.User)
                 .OrderByDescending(r => r.CreatedAt)
                 .FirstOrDefaultAsync(); // Get latest ride only

            if (ride == null)
                return null;

            // Determine pickup and drop-off stops
            var pickupStop = ride.RideStops.OrderBy(s => s.StopOrder).FirstOrDefault();
            var dropOffStop = ride.RideStops.OrderByDescending(s => s.StopOrder).FirstOrDefault();

            var lastRide = new LastRideDto
            {
                RideId = ride.RideId,
                UserId = ride.UserId,
                DriverId = ride.DriverId,
                PassengerId = ride.UserId,
                PassengerName = ride.User?.FullName,
                PassengerPhone = ride.User.PhoneNumber,
                RideType = ride.RideType,
                IsScheduled = ride.IsScheduled,
                ScheduledTime = ride.ScheduledTime,
                PassengerCount = ride.PassengerCount,
                FareEstimate = ride.FareEstimate,
                FareFinal = ride.FareFinal,
                Status = ride.Status,
                CreatedAt = ride.CreatedAt,
                Distance = Convert.ToDouble(ride.Distance),

                RideStartTime = ride.RideStartTime ?? TimeOnly.MinValue,
                RideEndTime = ride.RideEndTime ?? TimeOnly.MinValue,
                TotalWaitingTime = ride.RideWaitingTotalTime ?? TimeOnly.MinValue,

                // Pickup & DropOff details
                PickupLocation = pickupStop?.Location ?? string.Empty,
                PickupLat = pickupStop?.Latitude ?? 0,
                PickupLng = pickupStop?.Longitude ?? 0,
                DropOffLocation = dropOffStop?.Location ?? string.Empty,
                DropOffLat = dropOffStop?.Latitude ?? 0,
                DropOffLng = dropOffStop?.Longitude ?? 0,

                RideStops = ride.RideStops.Select(s => new RideStopsDto
                {
                    RideStopId = s.RideStopId,
                    StopOrder = s.StopOrder,
                    Location = s.Location,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude
                }).ToList()
            };

            return lastRide;
        }

        public async Task<object> GetUserLastRide(Guid userId)
        {
            var ride = await _context.Rides
                 .Where(r => r.UserId == userId)
                 .Include(r => r.RideStops)
                 .Include(r => r.User)
                 .Include(r => r.Driver)
                 .Include(r => r.Payments)
                 .OrderByDescending(r => r.CreatedAt)
                 .FirstOrDefaultAsync(); // Get latest ride only

            if (ride == null)
                return null;

            // Determine pickup and drop-off stops
            var pickupStop = ride.RideStops.OrderBy(s => s.StopOrder).FirstOrDefault();
            var dropOffStop = ride.RideStops.OrderByDescending(s => s.StopOrder).FirstOrDefault();

            var lastRide = new LastRideDto
            {
                RideId = ride.RideId,
                UserId = ride.UserId,
                DriverId = ride.DriverId,
                PassengerId = ride.UserId,
                PassengerName = ride.User?.FullName,
                PassengerPhone = ride.User?.PhoneNumber,
                RideType = ride.RideType,
                IsScheduled = ride.IsScheduled,
                ScheduledTime = ride.ScheduledTime,
                PassengerCount = ride.PassengerCount,
                FareEstimate = ride.FareEstimate,
                FareFinal = ride.FareFinal,
                Status = ride.Status,
                CreatedAt = ride.CreatedAt,
                Distance = Convert.ToDouble(ride.Distance),

                RideStartTime = ride.RideStartTime ?? TimeOnly.MinValue,
                RideEndTime = ride.RideEndTime ?? TimeOnly.MinValue,
                TotalWaitingTime = ride.RideWaitingTotalTime ?? TimeOnly.MinValue,

                // Pickup & DropOff details
                PickupLocation = pickupStop?.Location ?? string.Empty,
                PickupLat = pickupStop?.Latitude ?? 0,
                PickupLng = pickupStop?.Longitude ?? 0,
                DropOffLocation = dropOffStop?.Location ?? string.Empty,
                DropOffLat = dropOffStop?.Latitude ?? 0,
                DropOffLng = dropOffStop?.Longitude ?? 0,

                RideStops = ride.RideStops.Select(s => new RideStopsDto
                {
                    RideStopId = s.RideStopId,
                    StopOrder = s.StopOrder,
                    Location = s.Location,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude
                }).ToList(),
                PaymentIntentId = ride.Payments != null && ride.Payments.Count > 0 ? ride.Payments.First().PaymentToken : string.Empty,
                PaymentStatus = ride.Payments != null && ride.Payments.Count > 0 ? ride.Payments.First().PaymentStatus : string.Empty
            };

            return lastRide;
        }

        public async Task<string> CancelRideAsync(Guid rideId)
        {
            var ride = await _context.Rides.FirstOrDefaultAsync(r => r.RideId == rideId);
            if (ride == null)
            {
                return "Ride not found";
            }

            // Check if ride can be cancelled (not already completed or cancelled)
            if (ride.Status == "Completed")
            {
                return "Cannot cancel a completed ride";
            }

            if (ride.Status == "Cancelled")
            {
                return "Ride is already cancelled";
            }

            // Update ride status to Cancelled
            ride.Status = "Cancelled";
            _context.Rides.Update(ride);

            // If driver was assigned, set driver status back to Available
            if (ride.DriverId.HasValue)
            {
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == ride.DriverId.Value);
                if (driver != null)
                {
                    driver.Status = "Available";
                    _context.Drivers.Update(driver);
                }
            }

            await _context.SaveChangesAsync();
            return "Ride cancelled successfully";
        }
    }
}
