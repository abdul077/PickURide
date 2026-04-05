

using Microsoft.EntityFrameworkCore;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Models;
using PickURide.Application.Models.Drivers;
using PickURide.Infrastructure.Data;
using PickURide.Infrastructure.Data.Entities;
using System.Threading;

namespace PickURide.Infrastructure.Repositories
{
    public class DriverRepository : IDriverRepository
    {
        private readonly PickURideDbContext _context;

        public DriverRepository(PickURideDbContext context)
        {
            _context = context;
        }

        public Task<string> AcceptDriver(Guid driverId)
        {
            var driver = _context.Drivers.Where(m => m.DriverId == driverId).FirstOrDefault();
            if (driver == null)
            {
                return Task.FromResult("Driver not Found");
            }
            driver.ApprovalStatus = "Approved";
            _context.Drivers.Update(driver);
            _context.SaveChanges();
            return Task.FromResult("Driver Approved");
        }

        public async Task CreateAsync(DriverModel driver)
        {
            var dbUser = new Driver
            {
                DriverId = Guid.NewGuid(),
                FullName = driver.FullName,
                Email = driver.Email,
                PhoneNumber = driver.PhoneNumber,
                PasswordHash = driver.PasswordHash,
                CreatedAt = DateTime.UtcNow,
                //ApprovalStatus="Approved",
                StripeAccountId = driver.StripeAccountId,
                //Status= "Available",
                //Verified= true
            };

            _context.Drivers.Add(dbUser);
            await _context.SaveChangesAsync();
        }

        public async Task<List<DriverDto>> GetAllAvailableDriversAsync()
        {
            return await _context.Drivers
                .Where(d => d.Status == "Available" && d.Verified == true)
                .Select(d => new DriverDto
                {
                    DriverId = d.DriverId,
                    FullName = d.FullName,
                    PhoneNumber = d.PhoneNumber,
                    Email = d.Email,
                    Status = d.Status,
                    LicenseNumber = d.LicenseNumber,
                    VehicleColor = d.VehicleColor,
                    VehicleName = d.VehicleName,
                    StripeAccountId = d.StripeAccountId,
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<AllDriverDtos>> GetAllDriversAsync()
        {
            try
            {
                return await _context.Drivers
                    .Select(d => new AllDriverDtos
                    {
                        DriverId = d.DriverId,
                        Email = d.Email ?? string.Empty,
                        Address = d.Address ?? string.Empty,
                        Status = d.Status ?? string.Empty,
                        Verified = d.Verified ?? false,
                        CarLicensePlate = d.CarLicensePlate ?? string.Empty,
                        CreatedAt = d.CreatedAt,
                        FullName = d.FullName ?? string.Empty,
                        LicenseNumber = d.LicenseNumber ?? string.Empty,
                        PhoneNumber = d.PhoneNumber ?? string.Empty,
                        StripeAccountId = d.StripeAccountId ?? string.Empty,
                        ApprovalStatus = d.ApprovalStatus ?? string.Empty
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving drivers: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<AllDriverDtos>> GetApprovedDriversAsync()
        {
            return await _context.Drivers
                .Where(d => d.ApprovalStatus == "Approved" || d.ApprovalStatus == "Accepted")
                .Select(d => new AllDriverDtos
                {
                    DriverId = d.DriverId,
                    Email = d.Email,
                    Address = d.Address,
                    Status = d.Status,
                    Verified = d.Verified,
                    CarLicensePlate = d.CarLicensePlate,
                    CreatedAt = d.CreatedAt,
                    FullName = d.FullName,
                    LicenseNumber = d.LicenseNumber,
                    PhoneNumber = d.PhoneNumber,
                    StripeAccountId = d.StripeAccountId,
                    ApprovalStatus = d.ApprovalStatus
                })
                .ToListAsync();
        }

        public async Task<DriverDto?> GetAvailableDriverAsync()
        {
            var data = await _context.Drivers
               .Where(d => d.Status == "Available")
               .OrderBy(d => Guid.NewGuid()) // Random selection
               .FirstOrDefaultAsync();

            DriverDto dto = new DriverDto
            {
                CarLicensePlate = data.CarLicensePlate,
                DriverId = data.DriverId,
                Email = data.Email,
                FullName = data.FullName,
                PhoneNumber = data.PhoneNumber,
                Status = data.Status,
                LicenseNumber = data.LicenseNumber,
                StripeAccountId = data.StripeAccountId,

            };
            return dto;
        }

        public async Task<DriverModel?> GetByEmailAsync(string email)
        {
            var dbUser = await _context.Drivers.FirstOrDefaultAsync(u => u.Email == email);
            if (dbUser == null) return null;
            if (dbUser.ApprovalStatus == "Rejected")
            {
                return new DriverModel
                {
                    DriverId = dbUser.DriverId,
                    FullName = dbUser.FullName,
                    Email = dbUser.Email,
                    PhoneNumber = dbUser.PhoneNumber,
                    Verified = dbUser.Verified,
                    RejectedReason = dbUser.RejectionReason,
                    ApprovalStatus = dbUser.ApprovalStatus,
                    PasswordHash=dbUser.PasswordHash,
                    StripeAccountId = dbUser.StripeAccountId
                };
            }
            else
            {
                return new DriverModel
                {
                    DriverId = dbUser.DriverId,
                    FullName = dbUser.FullName,
                    Email = dbUser.Email,
                    PhoneNumber = dbUser.PhoneNumber,
                    PasswordHash = dbUser.PasswordHash,
                    Verified = dbUser.Verified,
                    ApprovalStatus=dbUser.ApprovalStatus,
                    StripeAccountId = dbUser.StripeAccountId,
                    Status=dbUser.Status
                };
            }

        }

        public async Task<DriverDto?> GetByIdAsync(Guid driverId)
        {
            var data = await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == driverId);
            if (data == null)
                return null;

            DriverDto dto = new DriverDto
            {
                CarLicensePlate = data.CarLicensePlate,
                DriverId = data.DriverId,
                Email = data.Email,
                FullName = data.FullName,
                PhoneNumber = data.PhoneNumber,
                Status = data.Status,
                LicenseNumber = data.LicenseNumber,
                StripeAccountId = data.StripeAccountId,

            };
            return dto;
        }

        public async Task<IEnumerable<DriverAttendanceDto>> GetDriverAttendancesAsync(Guid driverId)
        {
            return await _context.DriverAttendances
                .Where(a => a.DriverId == driverId)
                .Select(a => new DriverAttendanceDto
                {
                    AttendanceId = a.AttendanceId,
                    DriverId = a.DriverId,
                    AttendanceDate = a.CreatedAt,
                    Hours = a.EndTime.HasValue ? (a.EndTime.Value - a.StartTime).TotalHours : 0,
                    Present = !a.EndTime.HasValue ? true : (a.EndTime.Value - a.StartTime).TotalHours > 0
                })
                .ToListAsync();
        }

        public async Task<DriverModel?> GetDriverByEmailAsync(string email)
        {
            var driver = await _context.Drivers
             .AsNoTracking()
             .FirstOrDefaultAsync(d => d.Email == email);

            if (driver == null)
                return null;

            return new DriverModel
            {
                DriverId = driver.DriverId,
                FullName = driver.FullName,
                Email = driver.Email,
                PhoneNumber = driver.PhoneNumber,
                Address = driver.Address,
                LicenseNumber = driver.LicenseNumber,
                CarLicensePlate = driver.CarLicensePlate,
                CarVin = driver.CarVin,
                CarRegistration = driver.CarRegistration,
                CarInsurance = driver.CarInsurance,
                Sin = driver.Sin,
                LicenseImage = driver.LicenseImage,
                RegistrationImage = driver.RegistrationImage,
                InsuranceImage = driver.InsuranceImage,
                SelfieImage = driver.SelfieImage,
                Status = driver.Status,
                CreatedAt = driver.CreatedAt,
                PasswordHash = driver.PasswordHash,
                StripeAccountId = driver.StripeAccountId
            };
        }

        public async Task<IEnumerable<DriverLocationHistoryDto>> GetDriverLocationHistoriesAsync(Guid driverId)
        {
            return await _context.DriverLocationHistories
                .Where(l => l.DriverId == driverId)
                .Select(l => new DriverLocationHistoryDto
                {
                    DriverId = l.DriverId ?? Guid.Empty,
                    Latitude = Convert.ToDouble(l.Latitude),
                    Longitude = Convert.ToDouble(l.Longitude),
                    Timestamp = Convert.ToDateTime(l.LoggedAt)
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<DriverOvertimeDutyDto>> GetDriverOvertimeDutiesAsync(Guid driverId)
        {
            return await _context.DriverOvertimeDuties
           .Where(o => o.DriverId == driverId).
            Select(o => new DriverOvertimeDutyDto
            {
                OvertimeId = o.OvertimeDutyId,
                DriverId = o.DriverId,
                DutyDate = o.DutyStart,
                Hours = Convert.ToInt32((o.DutyEnd - o.DutyStart).TotalHours)
            })
           .ToListAsync();
        }

        public async Task<IEnumerable<DriverRideDto>> GetDriverRidesAsync(Guid driverId)
        {
            return await _context.Rides
                .Where(r => r.DriverId == driverId)
                .Select(r => new DriverRideDto
                {
                    RideId = r.RideId,
                    DriverId = r.DriverId ?? Guid.Empty,
                    UserId = r.UserId ?? Guid.Empty,
                    DropoffLocation = r.RideStops
                        .OrderByDescending(rs => rs.StopOrder)
                        .Select(rs => rs.Location.ToString())
                        .FirstOrDefault(),
                    DropoffLocationLatitude = r.RideStops
                        .OrderByDescending(rs => rs.StopOrder)
                        .Select(rs => rs.Latitude.HasValue ? rs.Latitude.Value.ToString() : null)
                        .FirstOrDefault(),
                    DropoffLocationLongitude = r.RideStops
                        .OrderByDescending(rs => rs.StopOrder)
                        .Select(rs => rs.Longitude.HasValue ? rs.Longitude.Value.ToString() : null)
                        .FirstOrDefault(),
                    PickupLocation = r.RideStops
                        .OrderBy(rs => rs.StopOrder)
                        .Select(rs => rs.Location)
                        .FirstOrDefault(),
                    PickupLocationLatitude = r.RideStops
                        .OrderBy(rs => rs.StopOrder)
                        .Select(rs => rs.Latitude.HasValue ? rs.Latitude.Value.ToString() : null)
                        .FirstOrDefault(),
                    PickupLocationLongitude = r.RideStops
                        .OrderBy(rs => rs.StopOrder)
                        .Select(rs => rs.Longitude.HasValue ? rs.Longitude.Value.ToString() : null)
                        .FirstOrDefault(),
                    RideDate = Convert.ToDateTime(r.CreatedAt),
                    Status = r.Status
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<DriverShiftApplicationDto>> GetDriverShiftApplicationsAsync(Guid driverId)
        {
            return await _context.DriverShiftApplications
                .Where(s => s.DriverId == driverId)
                .Select(s => new DriverShiftApplicationDto
                {
                    ApplicationId = s.ApplicationId,
                    DriverId = s.DriverId,
                    Status = s.Status,
                    AppliedDate = s.AppliedAt
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<DriverShiftDto>> GetDriverShiftsAsync(Guid driverId)
        {
            return await _context.DriverShifts
                .Where(s => s.DriverId == driverId)
                .Select(s => new DriverShiftDto
                {
                    ShiftId = s.ShiftId,
                    DriverId = s.DriverId ?? Guid.Empty,
                    ShiftEnd = Convert.ToDateTime(s.ShiftEnd),
                    ShiftStart = Convert.ToDateTime(s.ShiftStart),
                })
                .ToListAsync();
        }

        public Task<string> RejectDriver(Guid driverId, string RejectionReason)
        {
            var driver = _context.Drivers.Where(m => m.DriverId == driverId).FirstOrDefault();
            if (driver == null)
            {
                return Task.FromResult("Driver not Found");
            }
            driver.ApprovalStatus = "Rejected";
            driver.RejectionReason = RejectionReason;
            _context.Drivers.Update(driver);
            _context.SaveChanges();
            return Task.FromResult("Driver Rejected");
        }

        public async Task<bool> ResetPasswordAsync(Guid driverId, string newPassword)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(x => x.DriverId == driverId);
            if (driver == null) return false;

            driver.PasswordHash = newPassword;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> SetDriverAvailable(Guid driverId)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null)
            {
                return "Driver not Found";
            }
            //else if (driver.Status == "In-Ride")
            //{
            //    return "Can't update, still In-Ride";
            //}
            else
            {
                driver.Status = "Available";
                _context.Drivers.Update(driver);
                await _context.SaveChangesAsync();
                return "Status Updated";
            }
        }

        public async Task<string> SetDriverOffline(Guid driverId)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null)
            {
                return "Driver not Found";
            }
            else if (driver.Status == "In-Ride")
            {
                return "Can't update, still In-Ride";
            }
            else
            {
                driver.Status = "Offline";
                _context.Drivers.Update(driver);
                await _context.SaveChangesAsync();
                return "Status Updated";
            }
        }

        public async Task<bool> UpdateProfile(UpdateDriverProfileRequest request, CancellationToken cancellation)
        {
            var driver = await _context.Drivers
          .FirstOrDefaultAsync(x => x.DriverId == request.Id, cancellation);

            if (driver == null)
                return false;

            // Only update fields that are provided (not null)
            if (request.PhoneNumber != null)
                driver.PhoneNumber = request.PhoneNumber;
            
            if (request.Address != null)
                driver.Address = request.Address;
            
            if (request.LicenseNumber != null)
                driver.LicenseNumber = request.LicenseNumber;
            
            if (request.CarLicensePlate != null)
                driver.CarLicensePlate = request.CarLicensePlate;
            
            if (request.CarVin != null)
                driver.CarVin = request.CarVin;
            
            if (request.CarRegistration != null)
                driver.CarRegistration = request.CarRegistration;
            
            if (request.CarInsurance != null)
                driver.CarInsurance = request.CarInsurance;
            
            if (request.Sin != null)
                driver.Sin = request.Sin;
            
            if (request.VehicleName != null)
                driver.VehicleName = request.VehicleName;
            
            if (request.VehicleColor != null)
                driver.VehicleColor = request.VehicleColor;
            
            if (request.StripeAccountId != null)
                driver.StripeAccountId = request.StripeAccountId;

            await _context.SaveChangesAsync(cancellation);
            return true;
        }

        public async Task<bool> UpdateDriverDetailsAsync(UpdateDriverDetailsRequest request, CancellationToken cancellation)
        {
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(x => x.DriverId == request.DriverId, cancellation);

            if (driver == null)
                return false;

            bool imagesUpdated = false;

            if (!string.IsNullOrWhiteSpace(request.FullName))
                driver.FullName = request.FullName;

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                driver.PhoneNumber = request.PhoneNumber;

            if (!string.IsNullOrWhiteSpace(request.Address))
                driver.Address = request.Address;

            if (!string.IsNullOrWhiteSpace(request.LicenseNumber))
                driver.LicenseNumber = request.LicenseNumber;

            if (!string.IsNullOrWhiteSpace(request.CarLicensePlate))
                driver.CarLicensePlate = request.CarLicensePlate;

            if (!string.IsNullOrWhiteSpace(request.CarVin))
                driver.CarVin = request.CarVin;

            if (!string.IsNullOrWhiteSpace(request.CarRegistration))
                driver.CarRegistration = request.CarRegistration;

            if (!string.IsNullOrWhiteSpace(request.CarInsurance))
                driver.CarInsurance = request.CarInsurance;

            if (!string.IsNullOrWhiteSpace(request.Sin))
                driver.Sin = request.Sin;

            if (!string.IsNullOrWhiteSpace(request.VehicleName))
                driver.VehicleName = request.VehicleName;

            if (!string.IsNullOrWhiteSpace(request.VehicleColor))
                driver.VehicleColor = request.VehicleColor;

            if (!string.IsNullOrWhiteSpace(request.StripeAccountId))
                driver.StripeAccountId = request.StripeAccountId;

            if (!string.IsNullOrEmpty(request.LicenseImageBase64))
            {
                driver.LicenseImage = request.LicenseImageBase64;
                imagesUpdated = true;
            }

            if (!string.IsNullOrEmpty(request.RegistrationImageBase64))
            {
                driver.RegistrationImage = request.RegistrationImageBase64;
                imagesUpdated = true;
            }

            if (!string.IsNullOrEmpty(request.InsuranceImageBase64))
            {
                driver.InsuranceImage = request.InsuranceImageBase64;
                imagesUpdated = true;
            }

            if (!string.IsNullOrEmpty(request.SelfieImageBase64))
            {
                driver.SelfieImage = request.SelfieImageBase64;
                imagesUpdated = true;
            }

            if (imagesUpdated)
            {
                driver.ApprovalStatus = "Pending";
            }

            driver.Status = "Profile Updated";

            await _context.SaveChangesAsync(cancellation);
            return true;
        }

        public async Task<bool> UpdateStripeAccountIdAsync(Guid driverId, string stripeAccountId)
        {
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(x => x.DriverId == driverId);

            if (driver == null)
                return false;

            driver.StripeAccountId = stripeAccountId;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task UpdateToken(Guid Id, string Token)
        {
            var dbUser = await _context.Drivers.FindAsync(Id);
            if (dbUser == null)
            {
                return;
            }
            // Only update device token if provided (allows null)
            dbUser.DeviceToken = Token;
            dbUser.Status = "Available";
            _context.Drivers.Update(dbUser);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UploadImages(UploadDriverImagesRequest request, CancellationToken cancellationToken)
        {
            var driver = await _context.Drivers
             .FirstOrDefaultAsync(x => x.DriverId == request.DriverId, cancellationToken);

            if (driver == null || driver.Verified != true)
                return false;

            driver.LicenseImage = request.LicenseImageBase64;
            driver.RegistrationImage = request.RegistrationImageBase64;
            driver.InsuranceImage = request.InsuranceImageBase64;
            driver.SelfieImage = request.SelfieImageBase64;
            driver.ApprovalStatus="Pending";

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task Verified(string email, bool status)
        {
            var dbUser = await _context.Drivers.Where(m => m.Email == email).FirstOrDefaultAsync();
            if (dbUser == null)
            {
                return;
            }
            dbUser.Verified = status;
            _context.Drivers.Update(dbUser);
            await _context.SaveChangesAsync();
        }
        public Task DeleteUnverifiedByEmailAsync(string Email)
        {
            var unverifiedUsers = _context.Drivers.Where(u => u.Email == Email && (u.Verified == null || u.Verified == false)).ToList();
            if (unverifiedUsers.Any())
            {
                _context.Drivers.RemoveRange(unverifiedUsers);
                return _context.SaveChangesAsync();
            }
            return Task.CompletedTask;
        }

        public async Task<bool> DeleteDriverAsync(Guid driverId)
        {
            var driver = await _context.Drivers
                .Include(d => d.DriverAttendances)
                .Include(d => d.DriverLocationHistories)
                .Include(d => d.DriverOvertimeDuties)
                .Include(d => d.DriverShiftApplications)
                .Include(d => d.DriverShifts)
                .Include(d => d.Rides)
                .FirstOrDefaultAsync(d => d.DriverId == driverId);

            if (driver == null)
                return false;

            // Delete or handle related records
            // Delete driver attendances
            if (driver.DriverAttendances.Any())
            {
                _context.DriverAttendances.RemoveRange(driver.DriverAttendances);
            }

            // Delete driver location histories
            if (driver.DriverLocationHistories.Any())
            {
                _context.DriverLocationHistories.RemoveRange(driver.DriverLocationHistories);
            }

            // Delete driver overtime duties
            if (driver.DriverOvertimeDuties.Any())
            {
                _context.DriverOvertimeDuties.RemoveRange(driver.DriverOvertimeDuties);
            }

            // Delete driver shift applications
            if (driver.DriverShiftApplications.Any())
            {
                _context.DriverShiftApplications.RemoveRange(driver.DriverShiftApplications);
            }

            // Delete driver shifts
            if (driver.DriverShifts.Any())
            {
                _context.DriverShifts.RemoveRange(driver.DriverShifts);
            }

            // Set DriverId to null in Rides (preserve ride history)
            if (driver.Rides.Any())
            {
                foreach (var ride in driver.Rides)
                {
                    ride.DriverId = null;
                }
            }

            // Remove driver from database
            _context.Drivers.Remove(driver);
            await _context.SaveChangesAsync();
            return true;
        }

    }
}
