using EmailClassification.Application.Interfaces.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Infrastructure.Persistence.Repositories
{
    class EmailRepository : Repository<Email>, IEmailRepository
    {
        public EmailRepository(EmaildbContext context) : base(context)
        {
            
        }
    }
}
