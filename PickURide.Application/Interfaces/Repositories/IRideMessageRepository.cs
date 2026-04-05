using PickURide.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Interfaces.Repositories
{
    public interface IRideMessageRepository
    {
        Task AddRangeAsync(List<SaveRideMessageDto> messages);
    }
}
