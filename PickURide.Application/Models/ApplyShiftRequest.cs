using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class ApplyShiftRequest
    {
        public Guid ShiftId { get; set; }
        public Guid DriverId { get; set; }
    }
}
