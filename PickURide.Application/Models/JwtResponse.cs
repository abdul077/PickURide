using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class JwtResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Message { get; set; }
        public string? ApprovalStatus { get; set; }
        public string? StripeAccountId { get; set; }
        public string? RideStatus { get; set; }
    }
}
