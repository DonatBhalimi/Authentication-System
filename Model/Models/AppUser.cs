using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Models
{
    public class AppUser
    {
        public Guid Id {  get; set; }
        public string UserName {  get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedTime { get; set; } = DateTime.Now;
    }
}
