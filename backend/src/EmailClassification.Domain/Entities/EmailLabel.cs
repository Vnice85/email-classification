using System;
using System.Collections.Generic;

namespace EmailClassification.Infrastructure.Persistence;

public partial class EmailLabel
{
    public int LabelId { get; set; }

    public string? LabelName { get; set; }

    public virtual ICollection<Email> Emails { get; set; } = new List<Email>();
}
