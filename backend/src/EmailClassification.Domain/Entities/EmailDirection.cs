using System;
using System.Collections.Generic;

namespace EmailClassification.Infrastructure.Persistence;

public partial class EmailDirection
{
    public int DirectionId { get; set; }

    public string? DirectionName { get; set; }

    public virtual ICollection<Email> Emails { get; set; } = new List<Email>();
}
