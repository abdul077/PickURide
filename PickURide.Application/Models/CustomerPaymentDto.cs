using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class CustomerPaymentDto
    {
        public Guid? RideId { get; set; }
        public decimal? PaidAmount { get; set; }
        public decimal? TipAmount { get; set; }
        public Guid DriverId { get; set; }
        public Guid UserId { get; set; }
        public string? PaymentToken { get; set; }
    }
}
