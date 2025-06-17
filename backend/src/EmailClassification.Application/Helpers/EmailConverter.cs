using EmailClassification.Application.DTOs.Classification;
using MimeKit;

public static class EmailConverter
{
    public static byte[] ConvertToEml(EmailContent email)
    {
        var message = new MimeMessage();

        message.From.Add(MailboxAddress.Parse(email.From));
        message.To.Add(MailboxAddress.Parse(email.To));
        message.Subject = email.Subject;
        message.Date = email.Date;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = email.ContentType == "text/html" ? email.Body : null,
            TextBody = email.ContentType == "text/plain" ? email.Body : null
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var ms = new MemoryStream();
        message.WriteTo(ms);
        return ms.ToArray(); 
    }
}
