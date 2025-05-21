using EmailClassification.Application.DTOs.Guest;
using EmailClassification.Application.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EmailClassification.API.Controllers
{
    public class GuestController : BaseController
    {
        private IGuestService _guestService;

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

        [HttpGet("Messages")]
        public async Task<IActionResult> GetGuestEmails([FromQuery] GuestFilter filter)
        {
            var ls = await _guestService.GetGuestEmailsAsync(filter);
            return Ok(ls);
        }

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

        [HttpPost("Messages")]
        public async Task<IActionResult> SaveGuestEmail([FromBody] GuestEmailDTO guestEmail)
        {
            try
            {
                var model = await _guestService.AddGuestEmailAsync(guestEmail);
                return CreatedAtAction(nameof(GetGuestEmailById), new { id = model.EmailId }, model);
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpPut("Messages/{id}")]
        public async Task<IActionResult> EditGuestEmail(string id, [FromBody] GuestEmailDTO guestEmail)
        {
            var result = await _guestService.EditGuestEmailById(id, guestEmail);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

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

