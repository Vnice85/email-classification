using EmailClassification.Application.DTOs.Auth;
using EmailClassification.Application.Interfaces.IServices;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace EmailClassification.API.Controllers
{
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet("Login")]
        public async Task LoginWithGoogle()
        {
            await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme,
                new AuthenticationProperties
                {
                    RedirectUri = Url.Action("LoginCallback"),
                });
        }

        [HttpGet("Login/Callback")]
        public async Task<IActionResult> LoginCallback()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return StatusCode(401);
            }
            var test = await HttpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, "refresh_token");
            Console.WriteLine(test);
            var authInfo = new AuthInfoDTO
            {
                Email = result.Principal.FindFirst(ClaimTypes.Email)?.Value,
                Name = result.Principal.FindFirst(ClaimTypes.Name)?.Value,
                ProviderId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                GoogleAccessToken = await HttpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, "access_token"),
                GoogleRefreshToken = await HttpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, "refresh_token"),
                ExpiresAt = await HttpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, "expires_at"),
                ProfileImage = result.Principal.FindFirst("urn:google:picture")?.Value

            };
            var response = await _authService.LoginResponse(authInfo);
            var html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Logging in...</title>
                </head>
                <body>
                    <script>
                        (function() {{
                            const authData = {{
                                userId: '{response!.UserId}',
                                userName: '{response.UserName}',
                                jwt: '{response.JwtAccessToken}',
                                profileImage: '{response.ProfileImage}',
                                expiresAt: '{response.ExpiresAt}'
                            }};
                            window.opener?.postMessage(authData, '*');
                            window.close();
                        }})();
                    </script>
                    <p>Logging in...</p>
                </body>
                </html>";

            return Content(html, "text/html");
        }

        [Authorize]
        [HttpGet("RefreshToken")]
        public async Task<IActionResult> RefreshToken()
        {
            var userId = User.FindFirst(ClaimTypes.Email)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            var providerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)
               || string.IsNullOrEmpty(name)
               || string.IsNullOrEmpty(providerId))
            {
                return BadRequest("User not found");
            }
            var newToken = await _authService.RefreshAccessToken(userId, name, providerId);
            if (newToken == null)
            {
                return BadRequest("Failed to refresh token");
            }
            return Ok(new { jwtAccessToken = newToken.JwtAccessToken, expiresAt = newToken.ExpiresAt });

        }
    }
}
