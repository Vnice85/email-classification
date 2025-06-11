using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.Interfaces.Background
{
    public interface IBackgroundService
    {
        Task DeleteGuestAsync();
        Task SyncAllUsersEmails();
        Task ClassifyAllUsersEmails();
    }
}
