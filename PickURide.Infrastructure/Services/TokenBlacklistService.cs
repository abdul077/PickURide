using PickURide.Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Infrastructure.Services
{
    public class TokenBlacklistService : ITokenBlacklistService
    {
        public Task AddAsync(string token, DateTime expiryDate)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsBlacklistedAsync(string token)
        {
            throw new NotImplementedException();
        }
    }
}
