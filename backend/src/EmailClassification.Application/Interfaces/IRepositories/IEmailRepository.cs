using EmailClassification.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.Interfaces.IRepository
{
    public interface IEmailRepository : IRepository<Email>
    {
    }
}
