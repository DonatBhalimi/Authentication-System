using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Models
{
    public class TwoFactorCode
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public AppUser User { get; set; } = default!;
        public string Code { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
    }
}
