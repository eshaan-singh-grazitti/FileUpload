using FileUpload.Data;
using FileUpload.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FileUpload.Models;
using NPOI.HPSF;

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
            //var data = await _context.ExcelChanges.ToListAsync();
            var data = await _context.ExcelChanges
                .GroupBy(f => new { f.FileName, f.UserName }) // Group by FileName and UserName
                .Select(g => g.OrderByDescending(f => f.ChangeDate).FirstOrDefault()) // Select the latest record in each group
                .ToListAsync();
            return View(data);
        }
        [Authorize(Roles = "User,Admin")]
        public IActionResult UserDashboard()
        {
            return View();
        }
        //public IActionResult Changes(int id)
        //{
        //    var file  = _context.FileUploadModal.Find(id);
        //    var filename = file.Filename;
        //    string fileExt = Path.GetExtension(filename).ToLower();
        //    string filePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploads", filename);
        //    DTO.ExcelData = FileUploadController.PreviewExcel(filePath,fileExt);
        //    DTO.row = 
        //    DTO.column = 
        //    return View(DTO);
        //}
    }
}
