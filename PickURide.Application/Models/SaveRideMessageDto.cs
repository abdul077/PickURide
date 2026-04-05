using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class SaveRideMessageDto
    {
        public Guid RideId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderRole { get; set; } = "User";
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}
