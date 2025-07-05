using EmailClassification.Application.DTOs.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.Interfaces.IServices
{
    public interface IEmailService
    {
        Task<int> SendEmailAsync(SendEmailDTO email);
        Task<List<EmailHeaderDTO>> GetAllEmailsAsync(Filter filter);
        Task<EmailDetailDTO?> GetEmailByIdAsync(string emailId);

        Task<EmailDTO> SaveDraftEmailAsync(SendEmailDTO email);
        Task<EmailDTO?> UpdateDraftEmailByIdAsync(string draftId, SendEmailDTO email);

        Task<int> DeleteEmailAsync(string emailId);

        Task<int> SyncEmailsFromGmail(string userId, string directionName, bool syncAllEmails);
        Task ClassifyAllEmails(string userId);
        Task ClassifyAllEmailsByBatch(string userId);

        // helper method for sync option
        Task<bool> ExistHistoryId(string userId);
    }
}
