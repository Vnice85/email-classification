using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.DTOs.Auth
{
    public class AuthResponseDTO
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? JwtAccessToken { get; set; }
        public string? ProfileImage { get; set; }
        public string? ExpiresAt { get; set; }
    }
}
