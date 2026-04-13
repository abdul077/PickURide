using PickURide.Infrastructure.Data.Entities;
using PickURide.Infrastructure.Data;
using PickURide.Application.Models;
using Microsoft.EntityFrameworkCore;
using PickURide.Application.Interfaces.Repositories;
namespace PickURide.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly PickURideDbContext _context;

        public UserRepository(PickURideDbContext context)
        {
            _context = context;
        }
        public async Task<List<Users.GetUserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Include(u => u.Feedbacks)
                .Include(u => u.Rides)
                .Include(u => u.Tips)
                .AsNoTracking()
                .ToListAsync();

            var rideStops = await _context.RideStops.AsNoTracking().ToListAsync();

            return users.Select(u => new Users.GetUserDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                CreatedAt = u.CreatedAt,
                Verified = u.Verified,
                Feedbacks = u.Feedbacks.Select(f => new Users.GetUserFeedbackDto
                {
                    FeedbackId = f.FeedbackId,
                    RideId = f.RideId ?? Guid.Empty,
                    Rating = f.Rating ?? 0,
                    Comment = f.Comments,
                    CreatedAt = Convert.ToDateTime(f.CreatedAt)
                }).ToList(),
                Rides = u.Rides.Select(r =>
                {
                    var stops = rideStops.Where(rs => rs.RideId == r.RideId).ToList();
                    var pickupStop = stops.OrderBy(rs => rs.StopOrder).FirstOrDefault();
                    var dropoffStop = stops.OrderByDescending(rs => rs.StopOrder).FirstOrDefault();

                    return new Users.GetUserRideDto
                    {
                        RideId = r.RideId,
                        Status = r.Status,
                        CreatedAt = Convert.ToDateTime(r.CreatedAt),
                        PickupLocation = pickupStop?.Location,
                        PickupLocationLatituda = pickupStop?.Latitude?.ToString(),
                        PickupLocationLongitude = pickupStop?.Longitude?.ToString(),
                        DropoffLocation = dropoffStop?.Location,
                        DropoffLocationLatitude = dropoffStop?.Latitude?.ToString(),
                        DropoffLocationLongitude = dropoffStop?.Longitude?.ToString()
                    };
                }).ToList(),
                Tips = u.Tips.Select(t => new Users.GetUserTipDto
                {
                    TipId = t.TipId,
                    Amount = t.Amount ?? 0,
                    CreatedAt = Convert.ToDateTime(t.CreatedAt)
                }).ToList()
            }).ToList();
        }

        public async Task<Users.GetUserDto?> GetUserByIdAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Feedbacks)
                .Include(u => u.Rides)
                .Include(u => u.Tips)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return null;

            var rideStops = await _context.RideStops.AsNoTracking().ToListAsync();

            return new Users.GetUserDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                Verified = user.Verified,
                Feedbacks = user.Feedbacks.Select(f => new Users.GetUserFeedbackDto
                {
                    FeedbackId = f.FeedbackId,
                    RideId = f.RideId ?? Guid.Empty,
                    Rating = f.Rating ?? 0,
                    Comment = f.Comments,
                    CreatedAt = f.CreatedAt ?? DateTime.MinValue
                }).ToList(),
                Rides = user.Rides.Select(r =>
                {
                    var stops = rideStops.Where(rs => rs.RideId == r.RideId).ToList();
                    var pickupStop = stops.OrderBy(rs => rs.StopOrder).FirstOrDefault();
                    var dropoffStop = stops.OrderByDescending(rs => rs.StopOrder).FirstOrDefault();

                    return new Users.GetUserRideDto
                    {
                        RideId = r.RideId,
                        Status = r.Status,
                        CreatedAt = r.CreatedAt ?? DateTime.MinValue,
                        PickupLocation = pickupStop?.Location ?? "N/A",
                        PickupLocationLatituda = pickupStop?.Latitude?.ToString() ?? "",
                        PickupLocationLongitude = pickupStop?.Longitude?.ToString() ?? "",
                        DropoffLocation = dropoffStop?.Location ?? "N/A",
                        DropoffLocationLatitude = dropoffStop?.Latitude?.ToString() ?? "",
                        DropoffLocationLongitude = dropoffStop?.Longitude?.ToString() ?? ""
                    };
                }).ToList(),
                Tips = user.Tips.Select(t => new Users.GetUserTipDto
                {
                    TipId = t.TipId,
                    Amount = t.Amount ?? 0,
                    CreatedAt = t.CreatedAt ?? DateTime.MinValue
                }).ToList()
            };
        }
        public async Task<UsersModel?> GetByEmailAsync(string email)
        {
            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (dbUser == null) return null;

            return new UsersModel
            {
                UserId = dbUser.UserId,
                FullName = dbUser.FullName,
                Email = dbUser.Email,
                PhoneNumber = dbUser.PhoneNumber,
                PasswordHash=dbUser.PasswordHash,
                CreatedAt=dbUser.CreatedAt,
                Verified=dbUser.Verified
            };
        }

        public async Task CreateAsync(UsersModel userModel)
        {
            var dbUser = new User
            {
                UserId = Guid.NewGuid(),
                FullName = userModel.FullName,
                Email = userModel.Email,
                PhoneNumber = userModel.PhoneNumber,
                PasswordHash = userModel.PasswordHash
            };

            _context.Users.Add(dbUser);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateToken(Guid Id, string Token)
        {
            var dbUser =await _context.Users.FindAsync(Id);
            if (dbUser == null)
            {
                return;
            }
            dbUser.DeviceToken = Token;
            _context.Users.Update(dbUser);
            await _context.SaveChangesAsync();
        }

        public async Task Verified(string email, bool status)
        {
            var dbUser = await _context.Users.Where(m=>m.Email==email).FirstOrDefaultAsync();
            if (dbUser == null)
            {
                return;
            }
            dbUser.Verified = status;
            _context.Users.Update(dbUser);
            await _context.SaveChangesAsync();
        }

        public Task DeleteUnverifiedByEmailAsync(string Email)
        {
            var unverifiedUsers = _context.Users.Where(u => u.Email == Email && (u.Verified == null || u.Verified == false)).ToList();
            if (unverifiedUsers.Any())
            {
                _context.Users.RemoveRange(unverifiedUsers);
                return _context.SaveChangesAsync();
            }
            return Task.CompletedTask;
        }

        public Task UpdateAsync(UpdateUserModel user)
        {
            var dbUser = _context.Users.Find(user.UserId);
            if (dbUser == null)
            {
                return Task.CompletedTask;
            }
            dbUser.FullName = user.FullName;
            dbUser.PhoneNumber = user.PhoneNumber;
            dbUser.ProfilePicture=user.ProfileImage;
            _context.Users.Update(dbUser);
            return _context.SaveChangesAsync();
        }

        public Task<UpdateUserModel> SingleUser(Guid userId)
        {
            var dbUser = _context.Users.Find(userId);
            if (dbUser == null)
            {
                return Task.FromResult<UpdateUserModel>(null);
            }
            var userDto = new UpdateUserModel
            {
                UserId = dbUser.UserId,
                FullName =dbUser.FullName,
                PhoneNumber =dbUser.PhoneNumber,
                ProfileImage = dbUser.ProfilePicture
            };
            return Task.FromResult(userDto);
        }

        public Task<bool> ResetPasswordAsync(Guid Id, string newPassword)
        {
            var dbUser = _context.Users.Find(Id);
            if (dbUser == null)
            {
                return Task.FromResult(false);
            }
            dbUser.PasswordHash = newPassword;
            _context.Users.Update(dbUser);
            return Task.FromResult(_context.SaveChanges() > 0);
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Feedbacks)
                .Include(u => u.Rides)
                .Include(u => u.Tips)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return false;

            // Delete or handle related records
            // Delete user feedbacks
            if (user.Feedbacks.Any())
            {
                _context.Feedbacks.RemoveRange(user.Feedbacks);
            }

            // Delete user tips
            if (user.Tips.Any())
            {
                _context.Tips.RemoveRange(user.Tips);
            }

            // Set UserId to null in Rides (preserve ride history)
            if (user.Rides.Any())
            {
                foreach (var ride in user.Rides)
                {
                    ride.UserId = null;
                }
            }

            // Remove user from database
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string?> GetDeviceTokenAsync(Guid userId)
        {
            return await _context.Users.AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => u.DeviceToken)
                .FirstOrDefaultAsync();
        }
    }

}
