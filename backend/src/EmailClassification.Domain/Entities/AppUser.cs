using System;
using System.Collections.Generic;

namespace EmailClassification.Infrastructure.Persistence;

public partial class AppUser
{
    public string UserId { get; set; } = null!;

    public string? UserName { get; set; }

    public string? ProfileImage { get; set; }

    public virtual ICollection<Email> Emails { get; set; } = new List<Email>();

    public virtual ICollection<Token> Tokens { get; set; } = new List<Token>();
}
