using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class CreateShiftRequest
    {
        public DateTime ShiftDate { get; set; }
        public TimeOnly ShiftStart { get; set; }
        public TimeOnly ShiftEnd { get; set; }
        public int MaxDriverCount { get; set; }
        public string? Description { get; set; }
    }
}
