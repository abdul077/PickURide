using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class ChatMessageDto
    {
        public Guid RideId { get; set; }
        public Guid? DriverId { get; set; }
        public Guid SenderId { get; set; }
        public string? SenderRole { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
        public string? ChatType { get; set; }
    }

}
