using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class Dashboard
    {
        public int CompleteRide { get; set; }
        public int WaitingRides { get; set; }
        public int InProgressRide { get; set; }
        public decimal TotalIncome { get; set; }
        public int ActiveDrivers { get; set; }
        public int NoOfUsers { get; set; }
        public int NoOfDrivers { get; set; }
        public int TotalRides { get; set; }
        public decimal PaidIncomeDrivers { get; set; }
        public decimal AdminCommission { get; set; }
        public decimal DriverShares { get; set; }
        public decimal HeldPayments { get; set; }
    }
}
