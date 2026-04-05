using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Interfaces.Services
{
    public interface ITokenBlacklistService
    {
        Task AddAsync(string token, DateTime expiryDate);
        Task<bool> IsBlacklistedAsync(string token);
    }
}
