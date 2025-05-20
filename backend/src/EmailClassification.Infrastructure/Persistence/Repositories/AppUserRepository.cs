using EmailClassification.Application.Interfaces.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Infrastructure.Persistence.Repositories
{
    class AppUserRepository : Repository<AppUser>, IAppUserRepository
    {
        public AppUserRepository(EmaildbContext context) : base(context)
        {
        }
    }
}
