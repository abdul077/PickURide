using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models.Drivers
{
    public class AllDriverDtos
    {
        public Guid DriverId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? LicenseNumber { get; set; }
        public string? CarLicensePlate { get; set; }
        public string? Status { get; set; }
        public bool? Verified { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? StripeAccountId { get; set; }
        public string? ApprovalStatus { get; set; }
    }
}
