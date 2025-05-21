using EmailClassification.Application.DTOs.Guest;
using EmailClassification.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.Interfaces.IServices
{
    public interface IGuestService
    {
        Task<string> GenerateGuestIdAsync();
        Task<EmailDetailDTO?> GetGuestEmailByIdAsync(string id);
        Task<List<EmailDTO>> GetGuestEmailsAsync(GuestFilter filter);
        Task<EmailDTO> AddGuestEmailAsync(GuestEmailDTO email);
        Task<EmailDTO?> EditGuestEmailById(string id, GuestEmailDTO email);
        Task<bool> DeleteGuestEmailAsync(string emailId);

    }
}
