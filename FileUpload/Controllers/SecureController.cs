using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileUpload.Controllers
{
    public class SecureController : Controller
    {
        [Authorize(Roles = "Admin")]

        public IActionResult Admin()
        {
            return View();
        }
        [Authorize(Roles = "User,Admin")]

        public IActionResult User()
        {
            return View();
        }
    }
}
