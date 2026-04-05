using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Models;
using System;
using System.Threading.Tasks;

namespace PickURide.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Feedback : ControllerBase
    {
        private readonly IFeedbackRepository _feedbackService;

        public Feedback(IFeedbackRepository feedbackService)
        {
            _feedbackService = feedbackService;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (dto.Rating < 1 || dto.Rating > 5)
            {
                return BadRequest(new { Message = "Rating must be between 1 and 5." });
            }

            if (string.IsNullOrWhiteSpace(dto.FeedbackFrom) || 
                (dto.FeedbackFrom != "User" && dto.FeedbackFrom != "Driver"))
            {
                return BadRequest(new { Message = "FeedbackFrom must be either 'User' or 'Driver'." });
            }

            await _feedbackService.AddAsync(dto);
            return Ok(new { Message = "Feedback submitted." });
        }

        /// <summary>
        /// Get average rating (out of 5) for a given driver.
        /// </summary>
        [HttpPost("driver/{driverId:guid}/average-rating")]
        public async Task<IActionResult> GetDriverAverageRating(Guid driverId)
        {
            var avgRating = await _feedbackService.GetAverageRatingByDriverAsync(driverId);
            return Ok(new { DriverId = driverId, AverageRating = avgRating });
        }

        /// <summary>
        /// Get all feedback entries for a given driver.
        /// </summary>
        [HttpPost("driver/{driverId:guid}")]
        public async Task<IActionResult> GetFeedbackByDriver(Guid driverId)
        {
            var feedbacks = await _feedbackService.GetByDriverAsync(driverId);
            var averageRating = await _feedbackService.GetAverageRatingByDriverAsync(driverId);
            return Ok(new { AverageRating = averageRating, Feedbacks = feedbacks });
        }

        /// <summary>
        /// Get all feedback entries for a given user.
        /// </summary>
        [HttpPost("user/{userId:guid}")]
        public async Task<IActionResult> GetFeedbackByUser(Guid userId)
        {
            var feedbacks = await _feedbackService.GetByUserIdAsync(userId);
            var averageRating = await _feedbackService.GetAverageRatingByUserIdAsync(userId);
            return Ok(new { AverageRating = averageRating, Feedbacks = feedbacks });
        }

        /// <summary>
        /// Get all feedback entries for a given ride.
        /// </summary>
        [HttpPost("ride/{rideId:guid}")]
        public async Task<IActionResult> GetFeedbackByRide(Guid rideId)
        {
            var feedbacks = await _feedbackService.GetByRideAsync(rideId);
            return Ok(feedbacks);
        }

        /// <summary>
        /// Get all feedback entries.
        /// </summary>
        [HttpPost("all")]
        public async Task<IActionResult> GetAllFeedback()
        {
            var feedbacks = await _feedbackService.GetAllAsync();
            return Ok(feedbacks);
        }
    }
}
