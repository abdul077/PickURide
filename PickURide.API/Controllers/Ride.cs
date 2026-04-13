using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using PickURide.Application.Models.Drivers;

namespace PickURide.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Ride : ControllerBase
    {
        private readonly IRideService _rideService;

        public Ride(IRideService rideService)
        {
            _rideService = rideService;
        }

        [HttpPost("book")]
        public async Task<IActionResult> BookRide([FromBody] BookRideRequest request)
        {
            var rideId = await _rideService.BookRideAsync(request);
            return Ok(new { RideId = rideId });
        }

        [HttpPost("{rideId}")]
        public async Task<IActionResult> GetRide(Guid rideId)
        {
            var ride = await _rideService.GetRideByIdAsync(rideId);
            if (ride == null)
                return NotFound();
            return Ok(ride);
        }

        [HttpPost("{rideId}/end")]
        public async Task<IActionResult> EndRide(Guid rideId)
        {
            var ride = await _rideService.EndRideAsync(rideId);
            return Ok(new { Message = ride });
        }
        [HttpPost("{rideId}/start")]
        public async Task<IActionResult> StartRide(Guid rideId)
        {
            var ride = await _rideService.StartRideAsync(rideId);
            return Ok(new { Message = ride });
        }
        [HttpPost("assign-driver-admin")]
        public async Task<IActionResult> AssignDriverToRide([FromBody] AssignDriverRequest request)
        {
            var result = await _rideService.AssignDriverAsync(request.RideId, request.DriverId);
            return Ok(new { Message = result });
        }
        [HttpPost("add-ride-waiting-status")]
        public async Task<IActionResult> SetRideWaitingStatus(Guid rideId)
        {
            var ride = await _rideService.SetWaitingStatusAsync(rideId);
            return Ok(new { Message = ride });
        }
        [HttpPost("{rideId}/arrived")]
        public async Task<IActionResult> SetRideArrivedStatus(Guid rideId)
        {
            var ride = await _rideService.SetArrivedStatusAsync(rideId);
            return Ok(new { Message = ride });
        }
        [HttpPost("add-ride-waiting-time")]
        public async Task<IActionResult> SetRideWaitingTime(Guid rideId, string waitingTime, string status)
        {
            // Parse MM:SS format and convert to TimeOnly
            TimeOnly convertedTime;
            
            if (string.IsNullOrWhiteSpace(waitingTime))
            {
                return BadRequest(new { Message = "Waiting time cannot be empty." });
            }

            try
            {
                // Split the time string by colon
                var parts = waitingTime.Split(':');
                
                if (parts.Length != 2)
                {
                    return BadRequest(new { Message = "Invalid time format. Expected MM:SS format." });
                }

                // Parse minutes and seconds
                if (!int.TryParse(parts[0], out int minutes) || !int.TryParse(parts[1], out int seconds))
                {
                    return BadRequest(new { Message = "Invalid time values. Expected numbers." });
                }

                // Validate ranges
                if (minutes < 0 || seconds < 0 || seconds > 59)
                {
                    return BadRequest(new { Message = "Invalid time values. Minutes must be >= 0, seconds must be 0-59." });
                }

                // If minutes >= 60, convert to hours:minutes:seconds
                if (minutes >= 60)
                {
                    int hours = minutes / 60;
                    int remainingMinutes = minutes % 60;
                    convertedTime = new TimeOnly(hours, remainingMinutes, seconds);
                }
                else
                {
                    // If minutes < 60, use minutes as hours and seconds as minutes (or keep as is)
                    convertedTime = new TimeOnly(0, minutes, seconds);
                }

                var ride = await _rideService.SetWaitingTimeAsync(rideId, convertedTime, status);
                return Ok(ride);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Error parsing waiting time: {ex.Message}" });
            }
        }
        [HttpPost("get-all-rides-admin")]
        public async Task<IActionResult> GetAllRides(RidePaginationRequest request)
        {
            var rides = await _rideService.GetAllRidesAsync(request);
            return Ok(rides);
        }
        [HttpPost("get-ride-status-counts-admin")]
        public async Task<IActionResult> GetRideStatusCounts([FromBody] RidePaginationRequest? request)
        {
            var filterPeriod = request?.FilterPeriod ?? "all";
            var isScheduledFilter = request?.IsScheduledFilter;
            var counts = await _rideService.GetRideStatusCountsAsync(filterPeriod, isScheduledFilter);
            return Ok(counts);
        }
        [HttpPost("fare-estimate")]
        public async Task<IActionResult> FareEstimate(string Address, decimal distance, string duration, Guid? userId = null, string? promoCode = null)
        {
            var fare = await _rideService.FareEstimate(Address, distance, duration, userId, promoCode);
            return Ok(fare);
        }
        [HttpPost("get-driver-scheduled-rides-history")]
        public async Task<IActionResult> GetScheduledRidesHistory(Guid driverId)
        {
            var rides = await _rideService.GetScheduleRidesHistory(driverId);
            return Ok(rides);
        }
        [HttpPost("get-driver-rides-history")]
        public async Task<IActionResult> GetRidesHistory(Guid driverId)
        {
            var rides = await _rideService.GetRidesHistory(driverId);
            return Ok(rides);
        }
        [HttpPost("get-user-scheduled-rides-history")]
        public async Task<IActionResult> GetUserScheduledRidesHistory(Guid userId)
        {
            var rides = await _rideService.GetUserScheduleRidesHistory(userId);
            return Ok(rides);
        }
        [HttpPost("get-user-rides-history")]
        public async Task<IActionResult> GetUserRidesHistory(Guid userId)
        {
            var rides = await _rideService.GetUserRidesHistory(userId);
            return Ok(rides);
        }
        [HttpPost("get-user-completed-rides-history")]
        public async Task<IActionResult> GetUserCompletedRidesHistory(Guid userId)
        {
            var rides = await _rideService.GetUserCompletedRidesHistory(userId);
            return Ok(rides);
        }
        [HttpPost("get-driver-last-ride")]
        public async Task<IActionResult> GetDriverLastRide(Guid driverId)
        {
            var rides = await _rideService.GetDriverLastRide(driverId);
            return Ok(rides);
        }
        [HttpPost("cancel-ride")]
        public async Task<IActionResult> CancelRide(Guid rideId)
        {
            var result = await _rideService.CancelRideAsync(rideId);
            return Ok(new { Message = result });
        }
        [HttpPost("get-user-last-ride")]
        public async Task<IActionResult> GetUserLastRide(Guid userId)
        {
            var ride = await _rideService.GetUserLastRide(userId);
            if (ride is LastRideDto dto &&
                !string.IsNullOrWhiteSpace(dto.PaymentStatus) &&
                (string.Equals(dto.PaymentStatus, "completed", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(dto.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase)))
            {
                return NoContent();
            }

            return Ok(ride);
        }
    }
}
