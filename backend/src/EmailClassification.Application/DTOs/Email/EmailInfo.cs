namespace EmailClassification.Application.DTOs.Email;

public class EmailInfo
{
    public string? Body { get; set; }
    public string? PlainText { get; set; }
    public string? Snippet { get; set; }
    public DateTime? SentDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public string? FromAddress { get; set; }
    public string? ToAddress { get; set; }
    public string? Subject { get; set; }
    public string? HistoryId { get; set; }
}
