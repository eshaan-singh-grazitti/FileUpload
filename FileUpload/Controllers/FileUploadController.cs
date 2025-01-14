using DocumentFormat.OpenXml.Spreadsheet;
using FileUpload.Data;
using FileUpload.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.HSSF.UserModel;
using OfficeOpenXml;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Claims;

namespace FileUpload.Controllers
{
    public class FileUploadController : Controller
    {
        private readonly string[] ext = { ".png", ".gif", ".jpeg", ".bmp", ".webp", ".jpg", ".svg", ".xls", ".xlsx", ".zip" };
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        DataTransferModel DTO = new DataTransferModel();
        ExcelChangesViewModel EXM = new ExcelChangesViewModel();
        public FileUploadController(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            TempData["message"] = ViewBag.Message;
            var files = await _context.FileUploadModal.ToListAsync();
            return View(files);
        }
        [HttpGet]
        public async Task<IActionResult> DataGrid()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get logged-in user ID

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            else
            {
                bool isInRole = await _userManager.IsInRoleAsync(user, "User");
                if (isInRole)
                {
                    var userFiles = await _context.FileUploadModal
                        .Where(f => f.UserId == userId)
                        .Where(f => f.IsDeleted == false)
                        .OrderByDescending(f => f.UploadedOn)
                        .ToListAsync();

                    return View(userFiles);
                }
                else
                {
                    return View("DataGrid", _context.FileUploadModal.OrderByDescending(f => f.UploadedOn).ToList());
                }
            }


        }

        [HttpPost]
        [Microsoft.AspNetCore.Mvc.Route("/")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Upload(IFormFile file, int chunkIndex = 0, int totalChunks = 1)
        {
            try
            {
                if (file != null)
                {
                    string fileExt = Path.GetExtension(file.FileName).ToLower();

                    if (!Array.Exists(ext, e => e == fileExt))
                    {
                        ViewBag.Message = $"*Invalid file extension: {fileExt}. Allowed types are {string.Join(", ", ext)}.";
                        return View("Index", _context.FileUploadModal.OrderByDescending(f => f.UploadedOn).ToList());
                    }

                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
                    string fileName = $"{originalFileName}_{timestamp}{fileExt}";

                    // Temporary storage for chunks
                    string tempDir = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\TempChunks");
                    Directory.CreateDirectory(tempDir);

                    // Save the current chunk
                    string tempFilePath = Path.Combine(tempDir, $"{fileName}_chunk{chunkIndex}");

                    // Save the chunk asynchronously
                    using (FileStream tempStream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await file.CopyToAsync(tempStream);
                    }

                    string globalPath = Path.Combine(Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).FullName).FullName).FullName, "Uploaded Folder", fileName);
                    // If all chunks are uploaded, merge them in parallel
                    if (chunkIndex + 1 == totalChunks)
                    {
                        string finalPath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploads", fileName);
                        string compressedFilePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\CompressedUploads", fileName);

                        // Create a list of tasks for merging chunks in parallel
                        var tasks = new List<Task>();

                        // Merge chunks into the final file
                        using (FileStream finalFile = new FileStream(finalPath, FileMode.Create))
                        {
                            for (int i = 0; i < totalChunks; i++)
                            {
                                int chunkIndexToMerge = i; // Capture the correct index for each task
                                tasks.Add(Task.Run(async () =>
                                {
                                    string chunkPath = Path.Combine(tempDir, $"{fileName}_chunk{chunkIndexToMerge}");
                                    using (FileStream chunkStream = new FileStream(chunkPath, FileMode.Open))
                                    {
                                        await chunkStream.CopyToAsync(finalFile);
                                    }
                                    System.IO.File.Delete(chunkPath); // Delete the processed chunk
                                }));
                            }

                            // Wait for all chunk merge tasks to complete
                            await Task.WhenAll(tasks);
                        }

                        // Save to global path
                        System.IO.File.Copy(finalPath, globalPath, true);

                        // Compress the file for thumbnails (no change in logic)
                        if (fileExt != ".xls" && fileExt != ".xlsx")
                        {
                            if (fileExt == ".svg")
                            {
                                using (FileStream CompressedSvgPath = new FileStream(compressedFilePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(CompressedSvgPath);
                                }
                            }
                            else
                            {
                                using (var inputStream = file.OpenReadStream())
                                {
                                    using (var originalImage = Image.FromStream(inputStream))
                                    {
                                        // Set up compression parameters
                                        var jpegEncoder = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                                        var encoderParams = new EncoderParameters(1);
                                        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 1L); // Adjust quality (1-100)

                                        // Save compressed image to file
                                        using (var outputStream = new FileStream(compressedFilePath, FileMode.Create))
                                        {
                                            originalImage.Save(outputStream, jpegEncoder, encoderParams);
                                        }
                                    }
                                }
                            }
                        }
                        FileInfo fileInfo = new FileInfo(finalPath);
                        double fileSizeInBytes = fileInfo.Length;
                        double fileSizeInKB = fileSizeInBytes / 1024;
                        double fileSize = Math.Round(fileSizeInKB, 2);
                        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                        var fileUploadModal = new FileUploadModal
                        {
                            OriginalFilename = originalFileName + fileExt,
                            Filename = fileName,
                            FileType = file.ContentType,
                            Data = globalPath,
                            FileSize = fileSize,
                            CompressedPath = compressedFilePath,
                            Extention = fileExt,
                            UploadedOn = DateTime.Now,
                            UserId = userId,
                            IsDeleted = false
                        };
                        _context.FileUploadModal.Add(fileUploadModal);
                        await _context.SaveChangesAsync();
                        ViewBag.MessageSuccess = "File uploaded successfully";
                    }
                    else
                    {
                        ViewBag.MessageSuccess = $"Chunk {chunkIndex + 1}/{totalChunks} uploaded successfully.";
                    }

                    return View("Index", _context.FileUploadModal.OrderByDescending(f => f.UploadedOn).ToList());
                }
                else
                {
                    ViewBag.Message = "*Please select a file.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "*An error occurred while uploading the file. Please try again.";
                Console.WriteLine(ex.Message);
            }

            return RedirectToAction("Index");
        }

        public static List<Dictionary<string, string>> PreviewExcel(string filePath, string fileExt)
        {
            var data = new List<Dictionary<string, string>>();

            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found.");
            }

            if (fileExt == ".xlsx")
            {

                using var package = new ExcelPackage(new FileInfo(filePath));

                if (package.Workbook.Worksheets.Count == 0)
                {
                    throw new InvalidOperationException("No worksheets found in the Excel file.");
                }

                var worksheet = package.Workbook.Worksheets.First(); // First worksheet
                var rowCount = worksheet.Dimension?.Rows ?? 0;
                var colCount = worksheet.Dimension?.Columns ?? 0;

                // Extract headers
                var headers = new List<string>();
                for (int col = 1; col <= colCount; col++)
                {
                    headers.Add(worksheet.Cells[1, col].Text);
                }

                // Extract data
                for (int row = 2; row <= rowCount; row++) // Start from the second row (data rows)
                {
                    var rowData = new Dictionary<string, string>();
                    for (int col = 1; col <= colCount; col++)
                    {
                        var cellValue = worksheet.Cells[row, col].Text;
                        rowData[headers[col - 1]] = cellValue;
                    }
                    data.Add(rowData);
                }

            }
            if (fileExt == ".xls")
            {
                using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var workbook = new HSSFWorkbook(file);  // Use HSSFWorkbook for .xls filesx
                    var sheet = workbook.GetSheetAt(0);  // First sheet in the workbook

                    var headers = new List<string>();
                    var headerRow = sheet.GetRow(0);  // Read the first row as headers
                    for (int col = 0; col < headerRow.LastCellNum; col++) // Use LastCellNum to iterate only through valid cells
                    {
                        var cell = headerRow.GetCell(col); // Safely get the cell
                        headers.Add(cell?.ToString() ?? $"Column{col + 1}"); // Fallback to "ColumnX" if the cell is null
                    }

                    // Read the remaining rows
                    for (int row = 1; row <= sheet.LastRowNum; row++)
                    {
                        var rowData = new Dictionary<string, string>();
                        var dataRow = sheet.GetRow(row);
                        for (int col = 0; col < headers.Count; col++)
                        {
                            var cellValue = dataRow?.GetCell(col)?.ToString() ?? string.Empty;
                            rowData[headers[col]] = cellValue;
                        }
                        data.Add(rowData);
                    }
                }
            }
            return data;

        }

        [HttpPost]
        public IActionResult LoadExcelPreview(int fileId, string sortColumn, string sortDirection)
        {
            var file = _context.FileUploadModal.Find(fileId);
            if (file == null)
            {
                return BadRequest("File Not Found");
            }

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploads", file.Filename);

            // Get the Excel data
            var excelData = PreviewExcel(filePath, file.Extention);

            // Apply sorting if requested
            if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortDirection))
            {
                if (sortDirection.ToLower() == "asc")
                {
                    excelData = excelData.OrderBy(row =>
                    {
                        DTO.DirectionValue = "Ascending";
                        var value = row[sortColumn];
                        // Try to parse the value as a number
                        return decimal.TryParse(value, out var numericValue) ? (object)numericValue : value;
                    }).ToList();
                }
                else if (sortDirection.ToLower() == "desc")
                {
                    excelData = excelData.OrderByDescending(row =>
                    {
                        DTO.DirectionValue = "Descending";
                        var value = row[sortColumn];
                        // Try to parse the value as a number
                        return decimal.TryParse(value, out var numericValue) ? (object)numericValue : value;
                    }).ToList();
                }
            }
            DTO.SortedColumnName = sortColumn;
            DTO.ExcelValue = excelData;

            // Return the partial view with sorted data
            return PartialView("_PreviewExcel", DTO);
        }

        [HttpGet]
        public IActionResult SaveUpdatedExcel()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveUpdatedExcel([FromBody] DataRequest data)

        {
            try
            {
                string fileName = data.FileName;
                string ogFileName = data.OgFileName;
                List<List<string>> updatedData = data.UpdatedData;
                int fileid = data.Fileid;
                // Reconstruct the full file path
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploads", fileName);

                // Ensure the file exists
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                {
                    return BadRequest("File not found.");
                }

                // Determine file extension
                var fileExt = Path.GetExtension(filePath).ToLower();

                if (fileExt == ".xlsx")
                {
                    using var package = new ExcelPackage(new FileInfo(filePath));

                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        throw new InvalidOperationException("No worksheets found in the Excel file.");
                    }

                    var worksheet = package.Workbook.Worksheets.First(); // First worksheet
                    var rowCount = updatedData.Count;
                    var colCount = updatedData.FirstOrDefault()?.Count ?? 0;

                    // Update data in the worksheet
                    for (int row = 0; row < rowCount; row++)
                    {
                        for (int col = 0; col < colCount; col++)
                        {
                            var cellValue = worksheet.Cells[row + 2, col + 1].Text;
                            var newValue = updatedData[row][col].ToString();
                            if (cellValue != newValue)
                            {
                                var change = new ExcelChanges
                                {
                                    UserID = User.FindFirstValue(ClaimTypes.NameIdentifier),
                                    UserName = User.Identity.Name,
                                    FileId = fileid,
                                    FileName = ogFileName,
                                    Column = col,
                                    Row = row,
                                    OldValue = cellValue,
                                    NewValue = newValue,
                                    ChangeDate = DateTime.Now
                                };

                                await _context.ExcelChanges.AddAsync(change);

                                worksheet.Cells[row + 2, col + 1].Value = updatedData[row][col]; // Data starts from row 2
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                    // Save changes
                    package.Save();
                }
                else if (fileExt == ".xls")
                {
                    using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
                    {
                        var workbook = new HSSFWorkbook(file);
                        var sheet = workbook.GetSheetAt(0); // First sheet

                        // Update data in the sheet
                        for (int row = 0; row < updatedData.Count; row++)
                        {
                            var dataRow = sheet.GetRow(row + 1) ?? sheet.CreateRow(row + 1); // Data starts from row 1
                            for (int col = 0; col < updatedData[row].Count; col++)
                            {
                                var cell = dataRow.GetCell(col) ?? dataRow.CreateCell(col);
                                var cellValue = cell.ToString();
                                var newValue = updatedData[row][col].ToString();

                                if (cellValue != newValue)
                                {
                                    var change = new ExcelChanges
                                    {
                                        UserID = User.FindFirstValue(ClaimTypes.NameIdentifier),
                                        UserName = User.Identity.Name,
                                        FileId = fileid,
                                        FileName = fileName,
                                        Column = col,
                                        Row = row,
                                        OldValue = cellValue,
                                        NewValue = newValue,
                                        ChangeDate = DateTime.Now
                                    };


                                    await _context.ExcelChanges.AddAsync(change);
                                    cell.SetCellValue(updatedData[row][col]);
                                }
                            }
                        }
                        await _context.SaveChangesAsync();
                        // Save changes to the file
                        using (var outputFile = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            workbook.Write(outputFile);
                        }
                    }
                }
                else
                {
                    return BadRequest("Unsupported file format.");
                }

                return Json(new { success = true, message = "Excel file updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        public IActionResult Changes(int Id)
        {
            var file = _context.FileUploadModal.Find(Id);
            var file1 = _context.ExcelChanges
                .Where(f => f.FileId == Id)
                .GroupBy(f => new { f.FileName, f.UserID })
                .Select(g => g
                .OrderBy(f => f.Id)
                .ToList())
                .FirstOrDefault();
            var filename = file.Filename;
            string fileExt = Path.GetExtension(filename).ToLower();
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploads", filename);

            if (file1 != null && file1.Count > 0)
            {
                // Ensure EXM is not null

                EXM = new ExcelChangesViewModel
                {
                    ExcelData = PreviewExcel(filePath, fileExt),
                    row = new List<int>(),
                    column = new List<int>(),
                    ExcelChangesData = new List<ExcelChanges>()
                };


                for (var i = 0; i < file1.Count; i++)
                {
                    EXM.row.Add(file1[i].Row);
                    EXM.column.Add(file1[i].Column);
                    EXM.ExcelChangesData.Add(file1[i]);
                }
            }

            return View(EXM);
        }
        public async Task<IActionResult> Details(int id)
        {
            var file = await _context.FileUploadModal.FindAsync(id);
            if (file == null)
            {
                return BadRequest("File Not Found");
            }

            string fileName = Path.GetFileName(file.Filename);
            string fileExt = Path.GetExtension(file.Filename);

            // Check file type
            if (fileExt == ".xls" || fileExt == ".xlsx")
            {

                // Optional: Load initial preview (default unsorted)
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploads", file.Filename);
                var excelData = PreviewExcel(filePath, fileExt);
                DTO.ExcelValue = excelData;
            }
            else
            {
                // It will go to the details page.
            }

            return View(file);
        }

        public async Task<IActionResult> Download(int id)
        {
            var file = await _context.FileUploadModal.FindAsync(id);

            if (file == null)
            {
                return NotFound();
            }

            string finalPath = file.Data;
            var fileBytes = await System.IO.File.ReadAllBytesAsync(finalPath);

            return File(fileBytes, file.FileType, file.OriginalFilename);
        }

        public async Task<IActionResult> Delete(int id)
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); 
            var user = await _userManager.FindByIdAsync(userId);

            var file = await _context.FileUploadModal.FindAsync(id);
            if (file != null)
            {
                //string fileExt = file.Extention;
                //string filePath = file.Data;
                //string UploadFolder = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploads", file.Filename);
                //if (fileExt == ".xls" || fileExt == ".xlsx")
                //{
                //    if (System.IO.File.Exists(filePath) && System.IO.File.Exists(filePath))
                //    {
                //        System.IO.File.Delete(filePath);
                //        System.IO.File.Delete(UploadFolder);
                //    }
                //}
                //else
                //{
                //    string CompressedfilePath = file.CompressedPath;

                //    if (System.IO.File.Exists(filePath) && System.IO.File.Exists(CompressedfilePath) && System.IO.File.Exists(UploadFolder))
                //    {
                //        System.IO.File.Delete(filePath);
                //        System.IO.File.Delete(CompressedfilePath);
                //        System.IO.File.Delete(UploadFolder);
                //    }
                //}
                //_context.FileUploadModal.Remove(file);

                file.IsDeleted = true;
                if (user.UserName == "admin@abc.com")
                {
                    file.DeletedBy = "Admin";
                }
                else
                {
                    file.DeletedBy = user.UserName;
                }

                await _context.SaveChangesAsync();

                ViewBag.MessageSuccess = "File Deleted Successfully";
            }
            bool isInRole = await _userManager.IsInRoleAsync(user, "User");
            if (isInRole)
            {
                var userFiles = await _context.FileUploadModal
                    .Where(f => f.UserId == userId)
                    .Where(a => a.IsDeleted == false)
                    .OrderByDescending(f=>f.UploadedOn)
                    .ToListAsync();

                return View("DataGrid", userFiles);
            }
            else
            {
                return View("DataGrid", _context.FileUploadModal.OrderByDescending(f => f.UploadedOn).ToList());
            }
        }

    }
}