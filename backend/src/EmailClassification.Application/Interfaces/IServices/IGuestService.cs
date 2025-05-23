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
        Task<List<GuestEmailHeaderDTO>> GetGuestEmailsAsync(GuestFilter filter);
        Task<GuestEmailHeaderDTO> CreateGuestEmailAsync(CreateGuestEmailDTO email);
        Task<GuestEmailHeaderDTO?> EditGuestEmailById(string id, CreateGuestEmailDTO email);
        Task<bool> DeleteGuestEmailAsync(string emailId);
        Task<List<GuestEmailHeaderDTO>> SearchGuestEmailAsync(ElasticFilter filter);

    }
}
