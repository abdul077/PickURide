using System;
using System.Collections.Generic;

namespace PickURide.Infrastructure.Data.Entities;

public partial class BlacklistedToken
{
    public int Id { get; set; }

    public string TokenId { get; set; } = null!;

    public DateTime ExpiryDate { get; set; }
}
