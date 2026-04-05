using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Models;

namespace PickURide.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Tip : ControllerBase
    {
        private readonly ITipRepository _tipService;

        public Tip(ITipRepository tipService)
        {
            _tipService = tipService;
        }

        [HttpPost]
        public async Task<IActionResult> GiveTip([FromBody] TipDto dto)
        {
            var tip=await _tipService.AddAsync(dto);
            return Ok(new { Message = tip });
        }
    }
}
