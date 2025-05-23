using EmailClassification.Application.DTOs;
using EmailClassification.Application.DTOs.Guest;
using EmailClassification.Application.Interfaces.IServices;
using EmailClassification.Infrastructure.Attributes;
using EmailClassification.Infrastructure.Implement;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EmailClassification.API.Controllers
{
    public class GuestController : BaseController
    {
        private readonly IGuestService _guestService;

        public GuestController(IGuestService guestService)
        {
            _guestService = guestService;
        }

        [HttpGet("GuestId")]
        public async Task<IActionResult> GenerateGuestId()
        {
            var guestId = await _guestService.GenerateGuestIdAsync();
            return Ok(new { guestId });
        }

        [RequireGuestId]
        [HttpGet("Messages")]
        public async Task<IActionResult> GetGuestEmails([FromQuery] GuestFilter filter)
        {
            var ls = await _guestService.GetGuestEmailsAsync(filter);
            return Ok(ls);
        }

        [RequireGuestId]
        [HttpGet("Messages/{id}")]
        public async Task<IActionResult> GetGuestEmailById(string id)
        {
            var guestEmail = await _guestService.GetGuestEmailByIdAsync(id);
            if (guestEmail == null)
            {
                return NotFound();
            }

            return Ok(guestEmail);
        }

        [RequireGuestId]
        [HttpGet("Messages/Search")]
        public async Task<IActionResult> SearchGuestEmail([FromQuery] ElasticFilter filter)
        {
            var ls = await _guestService.SearchGuestEmailAsync(filter);
            return Ok(ls);
        }


        [RequireGuestId]
        [HttpPost("Messages")]
        public async Task<IActionResult> CreateGuestEmail([FromBody] CreateGuestEmailDTO guestEmail)
        {
            try
            {
                var model = await _guestService.CreateGuestEmailAsync(guestEmail);
                return CreatedAtAction(nameof(GetGuestEmailById), new { id = model.EmailId }, model);
            }
            catch
            {
                return BadRequest();
            }
        }

        [RequireGuestId]
        [HttpPut("Messages/{id}")]
        public async Task<IActionResult> EditGuestEmail(string id, [FromBody] CreateGuestEmailDTO guestEmail)
        {
            var result = await _guestService.EditGuestEmailById(id, guestEmail);
            if (result == null)
                return NotFound();
            return Ok(result);
        }


        [RequireGuestId]
        [HttpDelete("Messages/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _guestService.DeleteGuestEmailAsync(id);
            if (!result)
                return NotFound();
            return StatusCode(204);
        }
    }
}