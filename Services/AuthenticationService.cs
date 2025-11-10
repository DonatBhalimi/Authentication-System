using Data;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Model.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;


namespace Services
{
    public class AuthenticationService
    {
        private readonly AppDbContext _db;
        private readonly PasswordHasher<AppUser> _hasher = new();

        public AuthenticationService(AppDbContext db) => _db = db;

        public async Task<(bool ok, string error)> RegisterAsync(string userName, string email, string password)
        {
            if (!IsValidUserName(userName))
            {
                return (false, "Bad username.");
            }
            if (!IsValidEmail(email))
            {
                return (false, "Bad email.");
            }
            if (!IsValidPassword(password))
            {
                return (false, "Weak password.");
            }

            var exists = await _db.Users.AnyAsync(u => u.UserName == userName || u.Email == email);
            if (exists)
            {
                return (false, "Username or email already exists.");
            }

            var user = new AppUser { UserName = userName.Trim(), Email = email.Trim() };
            user.PasswordHash = _hasher.HashPassword(user, password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return (true, "");
        }

        public async Task<(AppUser? user, string error)> ValidateLoginAsync(string userOrEmail, string password)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.UserName == userOrEmail || u.Email == userOrEmail);

            if (user is null)
            {
                return (null, "Invalid credentials.");
            }

            var res = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            return res == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed ? (null, "Invalid credentials.") : (user, "");
        }


        public async Task<(bool ok, string error)> ChangePasswordAsync(Guid userId, string current, string next)

        {
            var user = await _db.Users.FindAsync(userId);
            if (user is null)
            {
                return (false, "User missing.");
            }

            var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, current);
            if (verify == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
            {
                return (false, "Password is wrong.");
            }

            if (!IsValidPassword(next))
            {
                return (false, "New password too weak.");
            }

            user.PasswordHash = _hasher.HashPassword(user, next);
            await _db.SaveChangesAsync();
            return (true, "");
        }
        public static ClaimsPrincipal CreatePrincipal(AppUser user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }
        private static bool IsValidUserName(string s) =>
            !string.IsNullOrWhiteSpace(s) && s.Length is >= 3 and <= 50;

        private static bool IsValidEmail(string s) =>
            !string.IsNullOrWhiteSpace(s) && s.Contains("@") && s.Length <= 100;

        private static bool IsValidPassword(string s)
        {
            if (string.IsNullOrWhiteSpace(s) || s.Length < 8) return false;
            var hasUpper = s.Any(char.IsUpper);
            var hasLower = s.Any(char.IsLower);
            var hasDigit = s.Any(char.IsDigit);
            return hasUpper && hasLower && hasDigit;
        }
    }
}
