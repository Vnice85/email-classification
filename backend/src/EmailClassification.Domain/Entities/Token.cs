using System;
using System.Collections.Generic;

namespace EmailClassification.Infrastructure.Persistence;

public partial class Token
{
    public int TokenId { get; set; }

    public string Provider { get; set; } = null!;

    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string UserId { get; set; } = null!;

    public virtual AppUser User { get; set; } = null!;
}
