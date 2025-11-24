using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Model.Models
{
    public class AppUser
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public bool IsEmailVerified { get; set; } = false;
        public bool IsTwoFactorEnabled { get; set; } = true;
        public DateTime CreatedTime { get; set; } = DateTime.Now;
    }
}
