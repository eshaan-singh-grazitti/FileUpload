using FileUpload.Data;
using FileUpload.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FileUpload.Controllers
{
    public class SecureController : Controller
    {
        private readonly AppDbContext _context;
        //DataTransferModel DTO = new DataTransferModel();
        ExcelChangesViewModel DTO = new ExcelChangesViewModel();
        public SecureController(AppDbContext context)
        {
            _context = context;
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            //var data = await _context.ExcelChanges.ToListAsync();
            var data = await _context.ExcelChanges
                .GroupBy(f => new { f.FileName, f.UserName }) // Group by FileName and UserName
                .Select(g => g.OrderBy(f => f.ChangeDate).FirstOrDefault()) // Select the latest record in each group
                .ToListAsync();
            return View(data);
        }
        [Authorize(Roles = "User,Admin")]
        public IActionResult UserDashboard()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

    }
}
