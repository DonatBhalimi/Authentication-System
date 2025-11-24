using DTO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;


namespace Authentication.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class AccountAPIController : ControllerBase
    {
        private readonly UserAuthService _auth;

        public AccountAPIController(UserAuthService auth)
        {
            _auth = auth;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest dto)
        {
            var (ok, error) = await _auth.RegisterAsync(dto.UserName, dto.Email, dto.Password);
            if (!ok) return BadRequest(error);
            return Ok("Registration step 1 completed. Verification code sent to email.");
        }

        [AllowAnonymous]
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailRequest dto)
        {
            var (ok, error) = await _auth.ConfirmEmailAsync(dto.Email, dto.Code);
            if (!ok) return BadRequest(error);
            return Ok("Email successfully verified. You can now log in.");
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> StartLogin(LoginRequest dto)
        {
            var (twoFactorId, error) = await _auth.StartLoginAsync(dto.UsernameOREmail, dto.Password);
            if (twoFactorId is null) return Unauthorized(error);
            return Ok(new { twoFactorId, message = "2FA code sent. Please verify using /api/account/verify-2fa." });
        }

        [AllowAnonymous]
        [HttpPost("verify-2fa")]
        public async Task<IActionResult> VerifyTwoFactor(VerifyTwoFactorRequest dto)
        {
            var (principal, error) = await _auth.CompleteTwoFactorLoginAsync(dto.TwoFactorId, dto.Code);
            if (principal == null) return Unauthorized(error);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = dto.RememberMe });
            return Ok("Login successful.");
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid user.");
            var (ok, error) = await _auth.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
            return ok ? Ok("Password changed.") : BadRequest(error);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok("Logged out.");
        }
    }
}
