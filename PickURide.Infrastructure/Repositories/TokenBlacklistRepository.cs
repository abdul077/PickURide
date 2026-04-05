using Microsoft.EntityFrameworkCore;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Models;
using PickURide.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Infrastructure.Repositories
{
    public class TokenBlacklistRepository : ITokenBlacklistRepository
    {
        private readonly PickURideDbContext _context;

        public TokenBlacklistRepository(PickURideDbContext context)
        {
            _context = context;
        }
        public async Task AddAsync(string token, DateTime expiryDate)
        {
            if (!await _context.BlacklistedTokens.AnyAsync(t => t.TokenId == token))
            {
                var entity = new Data.Entities.BlacklistedToken
                {
                    TokenId = token,
                    ExpiryDate = expiryDate
                };

                _context.BlacklistedTokens.Add(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsBlacklistedAsync(string token)
        {
            var record = await _context.BlacklistedTokens
           .FirstOrDefaultAsync(t => t.TokenId == token);

            if (record == null) return false;

            // Clean up expired tokens
            if (record.ExpiryDate < DateTime.UtcNow)
            {
                _context.BlacklistedTokens.Remove(record);
                await _context.SaveChangesAsync();
                return false;
            }

            return true;
        }
    }
}
