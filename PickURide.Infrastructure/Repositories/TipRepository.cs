using Microsoft.EntityFrameworkCore;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Models;
using PickURide.Infrastructure.Data;
using PickURide.Infrastructure.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Infrastructure.Repositories
{
    public class TipRepository : ITipRepository
    {
        private readonly PickURideDbContext _context;

        public TipRepository(PickURideDbContext context)
        {
            _context = context;
        }


        public async Task<string> AddAsync(TipDto tip)
        {
            // Check if the ride exists and is In-Progress
            var ride = await _context.Rides
                .Where(r => r.RideId == tip.RideId)
                .Select(r => new { r.RideId, r.Status })
                .FirstOrDefaultAsync();

            if (ride == null)
            {
                return "Ride not found.";
            }


            Tip tipEntity = new Tip
            {
                TipId = Guid.NewGuid(),
                RideId = tip.RideId,
                Amount = tip.Amount,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Tips.AddAsync(tipEntity);
            await _context.SaveChangesAsync();
            return "Tip added successfully.";
        }

        public async Task<string> GetTipbyRideId(Guid rideId)
        {
            var tip=await _context.Tips.Where(m=>m.RideId==rideId).FirstOrDefaultAsync();
            if (tip == null)
            {
                return "No Tip";
            }
            else
            {
                return tip.Amount.ToString();
            }
        }
    }
}
