using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login() => View();

        [HttpGet]
        public IActionResult Register() => View();

        [Authorize]
        [HttpGet]
        public IActionResult Profile() => View();

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword() => View();
    }
}
