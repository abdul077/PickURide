using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class UpdateDriverProfileRequest
    {
        public Guid Id { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? LicenseNumber { get; set; }
        public string? CarLicensePlate { get; set; }
        public string? CarVin { get; set; }
        public string? CarRegistration { get; set; }
        public string? CarInsurance { get; set; }
        public string? Sin { get; set; }
        public string? VehicleName { get; set; }
        public string? VehicleColor { get; set; }
        public string? StripeAccountId { get; set; }
    }
}
