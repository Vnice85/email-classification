using System;
using System.Collections.Generic;

namespace EmailClassification.Infrastructure.Persistence;

public partial class Email
{
    public string EmailId { get; set; } = null!;

    public string? UserId { get; set; }

    public string? FromAddress { get; set; }

    public string? ToAddress { get; set; }

    public DateTime? ReceivedDate { get; set; }

    public DateTime? SentDate { get; set; }

    public string? Subject { get; set; }

    public string? Body { get; set; }

    public int DirectionId { get; set; }

    public int? LabelId { get; set; }

    public string? HistoryId { get; set; }

    public string? Snippet { get; set; }

    public string? PlainText { get; set; }

    public virtual EmailDirection Direction { get; set; } = null!;

    public virtual EmailLabel? Label { get; set; }

    public virtual AppUser? User { get; set; }
}
