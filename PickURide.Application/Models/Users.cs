using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class Users
    {
        public class GetUserDto
        {
            public Guid UserId { get; set; }
            public string? FullName { get; set; }
            public string? Email { get; set; }
            public string? PhoneNumber { get; set; }
            public DateTime? CreatedAt { get; set; }
            public bool? Verified { get; set; }

            public List<GetUserFeedbackDto> Feedbacks { get; set; } = new();
            public List<GetUserRideDto> Rides { get; set; } = new();
            public List<GetUserTipDto> Tips { get; set; } = new();
        }

        public class GetUserFeedbackDto
        {
            public Guid FeedbackId { get; set; }
            public Guid RideId { get; set; }
            public int Rating { get; set; }
            public string? Comment { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class GetUserRideDto
        {
            public Guid RideId { get; set; }
            public string? Status { get; set; }
            public DateTime CreatedAt { get; set; }
            public string? PickupLocation { get; set; }
            public string? PickupLocationLatituda { get; set; }
            public string? PickupLocationLongitude { get; set; }
            public string? DropoffLocation { get; set; }
            public string? DropoffLocationLatitude { get; set; }
            public string? DropoffLocationLongitude { get; set; }
        }

        public class GetUserTipDto
        {
            public Guid TipId { get; set; }
            public decimal Amount { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}
