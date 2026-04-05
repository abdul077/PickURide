using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models.Drivers
{
    public class DriverRideDto
    {
        public Guid RideId { get; set; }
        public Guid DriverId { get; set; }
        public Guid UserId { get; set; }
        public string? PickupLocation { get; set; }
        public string? PickupLocationLatitude { get; set; }
        public string? PickupLocationLongitude { get; set; }
        public string? DropoffLocation { get; set; }
        public string? DropoffLocationLatitude { get; set; }
        public string? DropoffLocationLongitude { get; set; }
        public DateTime RideDate { get; set; }
        public string? Status { get; set; }
    }
}
