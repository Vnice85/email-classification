using System.ComponentModel.DataAnnotations;

namespace EmailClassification.Application.DTOs.Email;

public class SendEmailDTO
{
    [Required(ErrorMessage = "To address is required.")]
    [EmailAddress(ErrorMessage = "Incorrect email format")]
    public string? ToAddress { get; set; }

    public string? Subject { get; set; }

    public string? Body { get; set; }
}
