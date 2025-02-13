using DocumentFormat.OpenXml.Spreadsheet;
using FileUpload.Data;
using FileUpload.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FileUpload.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        public DashboardController(AppDbContext context)
        {
            _context = context;
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var handler = new JwtSecurityTokenHandler();
            var token = Request.Cookies["JwtToken"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Logout", "Account");
            }
            if (token == null)
            {
                token = Request.Cookies["JwtToken"];
            }
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            try
            {
                var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
                var userId = jsonToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Logout", "Account");
                }

                // Proceed with any further validation or logic
                var data = await _context.ExcelAuditTrail
                    .GroupBy(f => new { f.FileName, f.UserName }) // Group by FileName and UserName
                    .Select(g => g.OrderBy(f => f.ChangeDate).FirstOrDefault()) // Select the latest record in each group
                    .ToListAsync();
                return View(data);
            }
            catch (Exception)
            {
                return RedirectToAction("Logout", "Account"); // Handle any error in token validation
            }
        }
        [Authorize(Roles = "User,Admin")]
        public IActionResult UserDashboard()
        {
            var handler = new JwtSecurityTokenHandler();
            var token = Request.Cookies["JwtToken"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Logout", "Account");
            }
 

            if (token == null)
            {
                token = Request.Cookies["JwtToken"];
            }
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            try
            {
                var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
                var userId = jsonToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Logout", "Account");  // Handle invalid token
                }
               
                return View();
            }
            catch (Exception)
            {
                return RedirectToAction("Logout", "Account");  // Handle any error in token validation
            }
        }

    }
}
