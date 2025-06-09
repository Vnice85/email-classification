using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.DTOs.Auth
{
    public class AuthInfoDTO
    {
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? ProviderId { get; set; }
        public string? GoogleAccessToken { get; set; }
        public string? GoogleRefreshToken { get; set; }
        public string? ProfileImage { get; set; }
        public string? ExpiresAt { get; set; }
    }
}
