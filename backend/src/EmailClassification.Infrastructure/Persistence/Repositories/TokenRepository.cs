using EmailClassification.Application.Interfaces.IRepository;

namespace EmailClassification.Infrastructure.Persistence.Repositories
{
    public class TokenRepository : Repository<Token>, ITokenRepository
    {
        public TokenRepository(EmaildbContext context) : base(context)
        {
        }
    }
}
