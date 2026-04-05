using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class UploadDriverImagesRequest
    {
        public Guid DriverId { get; set; }
        public string? LicenseImageBase64 { get; set; }
        public string? RegistrationImageBase64 { get; set; }
        public string? InsuranceImageBase64 { get; set; }
        public string? SelfieImageBase64 { get; set; }
    }
}
