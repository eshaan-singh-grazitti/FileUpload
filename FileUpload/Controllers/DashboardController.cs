using FileUpload.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FileUpload.Controllers
{
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
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            //var data = await _context.ExcelChanges.ToListAsync();
            var data = await _context.ExcelAuditTrail
                .GroupBy(f => new { f.FileName, f.UserName }) // Group by FileName and UserName
                .Select(g => g.OrderBy(f => f.ChangeDate).FirstOrDefault()) // Select the latest record in each group
                .ToListAsync();
            return View(data);
        }
        [Authorize(Roles = "User,Admin")]
        public IActionResult UserDashboard()
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

    }
}
