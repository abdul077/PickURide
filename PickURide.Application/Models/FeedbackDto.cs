using System;
using System.ComponentModel.DataAnnotations;

namespace PickURide.Application.Models
{
    public class FeedbackDto
    {
        public Guid FeedbackId { get; set; }

        [Required]
        public Guid RideId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid DriverId { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        public string? Comments { get; set; }
        public DateTime CreatedAt { get; set; }

        [Required]
        public string FeedbackFrom { get; set; } = string.Empty; // "User" or "Driver"

        // For response only
        public string? DriverName { get; set; }
        public string? UserName { get; set; }
    }
}
