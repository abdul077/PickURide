using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class OtpEntry
    {
        public string Otp { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
        public bool Used { get; set; }
    }
}
