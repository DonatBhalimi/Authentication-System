using Data;
using DTO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services;
using System.Security.Claims;

namespace Authentication.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class AccountAPIController : ControllerBase
    {
        private readonly Services.AuthenticationService _auth;
        private readonly AppDbContext _db;
        public AccountAPIController(Services.AuthenticationService auth, AppDbContext db)
        {
            _auth = auth;
            _db = db;
        }


        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterRequest dto)
        {
            var (ok, error) = await _auth.RegisterAsync(dto.UserName, dto.Email, dto.Password);
            return ok ? Ok("Registered.") : BadRequest(error);
        }

        //cookie
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginRequest dto)
        {
            var (user, error) = await _auth.ValidateLoginAsync(dto.UsernameOREmail, dto.Password);
            if (user is null) return Unauthorized(error);

            await HttpContext.SignInAsync(Services.AuthenticationService.CreatePrincipal(user),
                new AuthenticationProperties { IsPersistent = dto.RememberCredentials });

            return Ok("Logged in.");
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

        // the protected endpoint for access for the user
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Ok("Logged out.");
        }
    }
}
