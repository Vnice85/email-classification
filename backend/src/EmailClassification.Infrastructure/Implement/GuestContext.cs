using EmailClassification.Application.Interfaces.IServices;

namespace EmailClassification.Infrastructure.Implement
{
    public class GuestContext : IGuestContext
    {
        public string? GuestId { get; set; } 
    }
}
