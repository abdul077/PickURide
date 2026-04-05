using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class RideHistoryResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int CompletedRides { get; set; }
        public decimal TotalFare { get; set; }
    }
}
