using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models.Drivers
{
    public class DriverTransactionDto
    {
        public string TotalPayment{ get; set; }
        public string PaidAmount { get; set; }
        public string TotalTrips { get; set; }
        public List<PaymentDto> Payment{ get; set; }
    }
}
