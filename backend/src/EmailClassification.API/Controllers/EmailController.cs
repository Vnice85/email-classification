using EmailClassification.Application.DTOs;
using EmailClassification.Application.DTOs.Email;
using EmailClassification.Application.Interfaces.IServices;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EmailClassification.API.Controllers;

[Authorize]
public class EmailController : BaseController
{
    private readonly IEmailService _emailService;
    private readonly IEmailSearchService _emailSearchService;

    public EmailController(IEmailService emailService, IEmailSearchService emailSearchService)
    {
        _emailService = emailService;
        _emailSearchService = emailSearchService;
    }

    [HttpPost("Messages/Send")]
    public async Task<IActionResult> SendEmail([FromBody] SendEmailDTO email)
    {
        var userId = User.FindFirst(ClaimTypes.Email)?.Value;
        if (userId == null)
        {
            return BadRequest("User not found");
        }
        var result = await _emailService.SendEmailAsync(email);
        return StatusCode(result);
    }

    [HttpGet("Messages")]
    public async Task<IActionResult> GetAllEmails([FromQuery] Filter filter)
    {
        var ls = await _emailService.GetAllEmailsAsync(filter);
        return Ok(ls);
    }
    [HttpGet("Messages/{id}")]
    public async Task<IActionResult> GetEmailById(string id)
    {
        var item = await _emailService.GetEmailByIdAsync(id);
        if (item == null)
        {
            return NotFound();
        }
        return Ok(item);
    }

    [HttpGet("Messages/Search")]
    public async Task<IActionResult> FindEmail([FromQuery] ElasticFilter filter)
    {
        var userId = User.FindFirst(ClaimTypes.Email)?.Value;
        if (userId == null)
        {
            return BadRequest("User not found");
        }
        var ls = await _emailSearchService.SearchAsync(userId, filter);
        return Ok(ls);
    }

    [HttpPost("Drafts/Save")]
    public async Task<IActionResult> SaveDraft([FromBody] SendEmailDTO email)
    {
        try
        {
            var result = await _emailService.SaveDraftEmailAsync(email);
            return CreatedAtAction(nameof(GetEmailById), new { id = result.EmailId }, result);
        }
        catch
        {
            return BadRequest();
        }
    }

    [HttpPut("Drafts/{id}")]
    public async Task<IActionResult> UpdateDraft(string id, [FromBody] SendEmailDTO email)
    {
        try
        {
            var result = await _emailService.UpdateDraftEmailByIdAsync(id, email);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
        catch
        {
            return BadRequest();
        }
    }

    [HttpDelete("Messages/{id}")]
    public async Task<IActionResult> DeleteEmail(string id)
    {
        var result = await _emailService.DeleteEmailAsync(id);
        return StatusCode(result);
    }


    [HttpGet("Sync")]
    public async Task SyncEmails()
    {
        var userId = User.FindFirst(ClaimTypes.Email)?.Value;
        if (userId == null)
        {
            return;
        }
        var existHistoryId = await _emailService.ExistHistoryId(userId);
        if (existHistoryId)
        {
            BackgroundJob.Enqueue<IEmailService>(e => e.SyncEmailsFromGmail(userId, "INBOX", false));
            BackgroundJob.Enqueue<IEmailService>(e => e.SyncEmailsFromGmail(userId, "SENT", false));
        }
        else
        {
            BackgroundJob.Enqueue<IEmailService>(e => e.SyncEmailsFromGmail(userId, "INBOX", true));
            BackgroundJob.Enqueue<IEmailService>(e => e.SyncEmailsFromGmail(userId, "SENT", true));
        }
    }

    [HttpGet("Classify")]
    public void ClassifyEmail()
    {
        var userId = User.FindFirst(ClaimTypes.Email)?.Value;
        if (userId == null)
            return;
        BackgroundJob.Enqueue<IEmailService>(e => e.ClassifyAllEmails(userId));
    }

}
