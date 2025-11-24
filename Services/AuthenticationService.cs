using Data;
using DTO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Authentication.Controllers
{
    [ApiController]
    [Route("api/internal/account")]
    public class InternalAccountAPIController : ControllerBase
    {
        private readonly UserAuthService _auth;
        private readonly AppDbContext _db;

        public InternalAccountAPIController(UserAuthService auth, AppDbContext db)
        {
            _auth = auth;
            _db = db;
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
            if (twoFactorId is null) return Unauthorized();
            return Ok(new { twoFactorId, message = "2FA code sent. Please verify using /api/account/verify-2fa." });
        }

        [AllowAnonymous]
        [HttpPost("verify-2fa")]
        public async Task<IActionResult> VerifyTwoFactor(VerifyTwoFactorRequest dto)
        {
            var (principal, error) = await _auth.CompleteTwoFactorLoginAsync(dto.TwoFactorId, dto.Code);
            if (principal == null) return Unauthorized();
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = dto.RememberMe });
            return Ok("Login successful.");
        }


        [Authorize]
        [HttpGet("usr")]
        public async Task<IActionResult> usr()
        {
            var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var usr = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == id);
            return Ok(new { usr.UserName, usr.Email, usr.CreatedTime });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest dto)
        {
            var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var (ok, error) = await _auth.ChangePasswordAsync(id, dto.CurrentPassword, dto.NewPassword);
            return ok ? Ok("Password changed.") : BadRequest(error);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Ok("Logged out.");
        }
    }
}
