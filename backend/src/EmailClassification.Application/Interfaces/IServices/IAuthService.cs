using EmailClassification.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.Interfaces.IServices
{
    public interface IAuthService
    {
        Task<AuthResponseDTO?> LoginResponse(AuthInfoDTO authInfo);
        Task<AuthResponseDTO?> RefreshAccessToken(string userId, string Name, string ProviderId);
    }
}
