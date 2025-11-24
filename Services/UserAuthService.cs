using Data;
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
    public class UserAuthService
    {
        private readonly AppDbContext _db;
        private readonly PasswordHasher<AppUser> _passwordHasher = new();
        private readonly IEmailSender _email;

        public UserAuthService(AppDbContext db, IEmailSender email)
        {
            _db = db;
            _email = email;
        }

        public async Task<(bool ok, string error)> RegisterAsync(string username, string email, string password)
        {
            if (!IsValidUserName(username)) return (false, "Invalid username.");
            if (!IsValidEmail(email)) return (false, "Invalid email.");
            if (!IsValidPassword(password)) return (false, "Weak password.");

            var exists = await _db.Users
                .AnyAsync(x => x.UserName == username || x.Email == email);

            if (exists) return (false, "Username or email already in use.");

            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = username,
                Email = email
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            await _db.Users.AddAsync(user);

            var code = GenerateNumericCode(6);

            var verification = new EmailVerification
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false
            };

            await _db.EmailVerifications.AddAsync(verification);
            await _db.SaveChangesAsync();

            await _email.SendAsync(email, "Verification Code", $"Your verification code is: {code}");

            return (true, "");
        }

        public async Task<(bool ok, string error)> ConfirmEmailAsync(string email, string code)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null) return (false, "User not found.");

            if (user.IsEmailVerified) return (true, "");

            var record = await _db.EmailVerifications
                .Where(v => v.UserId == user.Id && !v.IsUsed && v.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(v => v.ExpiresAt)
                .FirstOrDefaultAsync();

            if (record == null || !string.Equals(record.Code, code, StringComparison.Ordinal))
                return (false, "Invalid or expired verification code.");

            record.IsUsed = true;
            user.IsEmailVerified = true;

            await _db.SaveChangesAsync();
            return (true, "");
        }

        public async Task<(Guid? twoFactorId, string error)> StartLoginAsync(string userOrEmail, string password)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.UserName == userOrEmail || u.Email == userOrEmail);

            if (user == null)
                return (null, "Invalid credentials.");

            if (!user.IsEmailVerified)
                return (null, "Email not verified.");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
                return (null, "Invalid credentials.");

            var code = GenerateNumericCode(6);

            var twoFactor = new TwoFactorCode
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false
            };

            await _db.TwoFactorCodes.AddAsync(twoFactor);
            await _db.SaveChangesAsync();

            await _email.SendAsync(user.Email, "2FA Code", $"Your 2FA code is: {code}");

            return (twoFactor.Id, "");
        }

        public async Task<(ClaimsPrincipal? principal, string error)> CompleteTwoFactorLoginAsync(Guid twoFactorId, string code)
        {
            var record = await _db.TwoFactorCodes
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == twoFactorId);

            if (record == null ||
                record.IsUsed ||
                record.ExpiresAt <= DateTime.UtcNow ||
                !string.Equals(record.Code, code, StringComparison.Ordinal))
            {
                return (null, "Invalid or expired 2FA code.");
            }

            if (record.User == null)
                return (null, "User not found.");

            record.IsUsed = true;
            await _db.SaveChangesAsync();

            var principal = CreatePrincipal(record.User);
            return (principal, "");
        }

        public async Task<(bool ok, string error)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return (false, "User not found.");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
            if (result == PasswordVerificationResult.Failed)
                return (false, "Current password incorrect.");

            if (!IsValidPassword(newPassword))
                return (false, "New password does not meet requirements.");

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            await _db.SaveChangesAsync();

            return (true, "");
        }

        public ClaimsPrincipal CreatePrincipal(AppUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            return new ClaimsPrincipal(identity);
        }

        private static string GenerateNumericCode(int length)
        {
            var rng = new Random();
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                sb.Append(rng.Next(0, 10));
            return sb.ToString();
        }

        private static bool IsValidUserName(string s) =>
            !string.IsNullOrWhiteSpace(s) && s.Length <= 50;

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
