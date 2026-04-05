using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class BlacklistedToken
    {
        public int Id { get; set; }
        public string TokenId { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
    }
}
