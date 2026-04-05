using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Interfaces.Repositories
{
    public interface ITokenBlacklistRepository
    {
        Task AddAsync(string token, DateTime expiryDate);
        Task<bool> IsBlacklistedAsync(string token);
    }
}
