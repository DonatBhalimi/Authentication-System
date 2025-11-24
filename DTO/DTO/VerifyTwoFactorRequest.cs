using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DTO
{
    public class VerifyTwoFactorRequest
    {
        public Guid TwoFactorId { get; set; }
        public string Code { get; set; } = default!;
        public bool RememberMe { get; set; }
    }
}
