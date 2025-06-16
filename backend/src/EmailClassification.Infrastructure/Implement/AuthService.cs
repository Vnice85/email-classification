using Azure.Core;
using EmailClassification.Application.DTOs.Auth;
using EmailClassification.Application.Helpers;
using EmailClassification.Application.Interfaces;
using EmailClassification.Application.Interfaces.IServices;
using EmailClassification.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EmailClassification.Infrastructure.Implement
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IConfiguration configuration,
                           IUnitOfWork unitOfWork,
                           HttpClient httpClient,
                           ILogger<AuthService> logger)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _httpClient = httpClient;
            _logger = logger;
        }
        public async Task<AuthResponseDTO?> LoginResponse(AuthInfoDTO authInfo)
        {
            if (authInfo == null
                || string.IsNullOrEmpty(authInfo.Email)
                || string.IsNullOrEmpty(authInfo.Name)
                || string.IsNullOrEmpty(authInfo.ProviderId))
            {
                return null;
            }

            var user = await _unitOfWork.AppUser.GetItemWhere(u => u.UserId == authInfo.Email);
            await _unitOfWork.BeginTransactionASync();
            try
            {
                if (user == null)
                {
                    var userItem = new AppUser()
                    {
                        UserId = authInfo.Email,
                        UserName = authInfo.Name,
                        ProfileImage = authInfo.ProfileImage
                    };
                    await _unitOfWork.AppUser.AddAsync(userItem);
                    await _unitOfWork.SaveAsync();
                }
                var jwtAccessToken = GenerateJwtToken(authInfo.Email, authInfo.Name, authInfo.ProviderId);
                var tokenItem = await _unitOfWork.Token.GetItemWhere(u => u.UserId == authInfo.Email && u.Provider == "GOOGLE");
                if (tokenItem == null)
                {
                    var token = new Token()
                    {
                        Provider = "GOOGLE",
                        AccessToken = AesHelper.Encrypt(authInfo.GoogleAccessToken ?? "", _configuration["Aes:Key"] ?? ""),
                        RefreshToken = AesHelper.Encrypt(authInfo.GoogleRefreshToken ?? "", _configuration["Aes:Key"] ?? ""),
                        ExpiresAt = DateTime.UtcNow.AddHours(8),
                        UserId = authInfo.Email,
                    };
                    await _unitOfWork.Token.AddAsync(token);
                }
                else
                {
                    if (!string.IsNullOrEmpty(authInfo.GoogleRefreshToken))
                    {
                        tokenItem.RefreshToken = AesHelper.Encrypt(authInfo.GoogleRefreshToken ?? "", _configuration["Aes:Key"] ?? "");
                    }
                    tokenItem.AccessToken = AesHelper.Encrypt(authInfo.GoogleAccessToken ?? "", _configuration["Aes:Key"] ?? "");
                    tokenItem.ExpiresAt = DateTime.UtcNow.AddMinutes(60);
                    _unitOfWork.Token.Update(tokenItem);
                }
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
                if (DateTime.TryParse(authInfo.ExpiresAt, out var tmp))
                {
                    authInfo.ExpiresAt = tmp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                }

                return new AuthResponseDTO
                {
                    UserId = authInfo.Email,
                    UserName = authInfo.Name,
                    JwtAccessToken = jwtAccessToken,
                    ProfileImage = authInfo.ProfileImage,
                    ExpiresAt = authInfo.ExpiresAt
                };
            }
            catch(Exception ex)
            {
                _logger.LogError("Error occurred while processing login response for user {ex}", ex);
                throw;
            }

        }
       

        public async Task<AuthResponseDTO?> RefreshAccessToken(string userId, string Name, string ProviderId)
        {
            var token = await GetNewAccessToken(userId);
            if (token == null)
            {
                return null;
            }
            var oldToken = await _unitOfWork.Token.GetItemWhere(u => u.UserId == userId && u.Provider.ToUpper() == "GOOGLE");
            if (oldToken == null)
            {
                var newToken = new Token()
                {
                    Provider = "GOOGLE",
                    AccessToken = token.AccessToken,
                    ExpiresAt = token.ExpiresAt,
                    UserId = userId
                };
                await _unitOfWork.Token.AddAsync(newToken);
                await _unitOfWork.SaveAsync();
            }
            else
            {
                oldToken.AccessToken = token.AccessToken;
                oldToken.ExpiresAt = token.ExpiresAt;
                _unitOfWork.Token.Update(oldToken);
                await _unitOfWork.SaveAsync();
            }
            var jwtAccessToken = GenerateJwtToken(userId, Name, ProviderId);
            return new AuthResponseDTO
            {
                UserId = userId,
                UserName = Name,
                JwtAccessToken = jwtAccessToken,
                ExpiresAt = DateTimeHelper.FormatToVietnamTime(token.ExpiresAt)
            };
        }


        private async Task<Token?> GetNewAccessToken(string userId)
        {
            var tokenItem = await _unitOfWork.Token.GetItemWhere(r => r.UserId == userId && r.Provider.ToUpper() == "GOOGLE");
            if (tokenItem == null || tokenItem.RefreshToken == null)
            {
                return null;
            }
            var refreshToken = AesHelper.Decrypt(tokenItem.RefreshToken, _configuration["Aes:Key"] ?? "");
            var clientId = _configuration["Authentication:Google:ClientId"];
            var clientSecret = _configuration["Authentication:Google:ClientSecret"];
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogError("ClientId or ClientSecret is not configured in the application settings.");
                throw new ArgumentException("ClientId or ClientSecret is not configured");
            }
            var requestData = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "refresh_token", refreshToken! },
                { "grant_type", "refresh_token" }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
            {
                Content = new FormUrlEncodedContent(requestData)
            };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);
            var accessToken = json["access_token"]?.ToString();
            var token = new Token
            {
                AccessToken = AesHelper.Encrypt(accessToken ?? "", _configuration["Aes:Key"] ?? ""),
                ExpiresAt = DateTime.UtcNow.AddSeconds((int)json["expires_in"]!)
            };
            return token;
        }


        private string GenerateJwtToken(string email, string name, string providerId)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Authentication:Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Name, name),
                    new Claim(ClaimTypes.NameIdentifier, providerId)
                };
            var token = new JwtSecurityToken(
                issuer: _configuration["Authentication:Jwt:Issuer"],
                audience: _configuration["Authentication:Jwt:Audience"],
                claims: claims,
                // i set the expire time of jwt equal with expire time of access_token of google (60 minutues)
                // after 60 minutes, jwt and access_token of google will be expired, fe will call refresh token api to get new access_token and jwt
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Authentication:Jwt:ExpiryInMinutes"])),
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }


    }
}
