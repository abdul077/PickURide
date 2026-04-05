using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class DriverDto
    {
        public Guid DriverId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Status { get; set; }
        public string? CarLicensePlate { get; set; }
        public string? LicenseNumber { get; set; }
        public string? VehicleName { get; set; }
        public string? VehicleColor { get; set; }
        public string? StripeAccountId { get; set; }
    }
}
