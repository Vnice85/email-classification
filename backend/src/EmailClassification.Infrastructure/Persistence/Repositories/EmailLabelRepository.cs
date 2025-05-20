using EmailClassification.Application.Interfaces.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Infrastructure.Persistence.Repositories
{
    class EmailLabelRepository : Repository<EmailLabel>, IEmailLabelRepository
    {
        public EmailLabelRepository(EmaildbContext context) : base(context)
        {
        }
    }
}
