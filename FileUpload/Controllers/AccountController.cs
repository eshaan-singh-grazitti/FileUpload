using FileUpload.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FileUpload.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new IdentityUser { UserName = model.UserName, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);


            if (result.Succeeded)
            {
                user.EmailConfirmed = true;
                await _userManager.AddToRoleAsync(user, "User");
                return RedirectToAction("Login", "Account");
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Attempt to sign in the user
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    var roleUser = await _userManager.GetRolesAsync(user);
                    if (roleUser.Count == 1)
                    {
                        var role = roleUser[0];
                        if (role == "Admin")
                        {
                            // Redirect to User page
                            return RedirectToAction("Admin", "Secure");
                        }
                        else
                        {
                            return RedirectToAction("User", "Secure");
                        }
                    }
                    
                }

            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }
        public async Task<IActionResult> Logout(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");  // Or redirect to any other page
        }
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
