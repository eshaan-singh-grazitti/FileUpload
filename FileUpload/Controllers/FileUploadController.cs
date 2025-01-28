﻿using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using FileUpload.Data;
using FileUpload.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.HPSF;
using NPOI.HSSF.Record.Chart;
using NPOI.HSSF.UserModel;
using NPOI.OpenXmlFormats.Spreadsheet;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
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
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            TempData["message"] = ViewBag.Message;
            var files = await _context.FileUploadModal.ToListAsync();
            return View(files);
        }
        [HttpGet]
        public async Task<IActionResult> DataGrid()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

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

                    if (!System.Array.Exists(ext, e => e == fileExt))
                    {
                        ViewBag.Message = $"*Invalid file extension: {fileExt}. Allowed types are {string.Join(", ", ext)}.";
                        return View("Index", _context.FileUploadModal.OrderByDescending(f => f.UploadedOn).ToList());
                    }

                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
                    string fileName = $"{originalFileName}_{timestamp}{fileExt}";
                    string finalPath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploads", fileName);


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
                        if (fileExt == ".xls")
                        {
                            using (FileStream fileDemo = new FileStream(finalPath, FileMode.Open, FileAccess.Read))
                            {
                                var workbook = new HSSFWorkbook(fileDemo);  // Use HSSFWorkbook for .xls filesx
                                var sheet = workbook.GetSheetAt(0);  // First sheet in the workbook

                                var headers = new List<string>();
                                var headerRow = sheet.GetRow(0);  // Read the first row as headers
                                if (headerRow == null)
                                {
                                    System.IO.File.Delete(finalPath);
                                    ViewBag.Message = "*Excel file is empty.";
                                    return View("Index", _context.FileUploadModal.OrderByDescending(f => f.UploadedOn).ToList());
                                }

                            }
                        }
                        if (fileExt == ".xlsx")
                        {
                            using var package = new ExcelPackage(new FileInfo(finalPath));

                            var worksheet = package.Workbook.Worksheets.First(); // Get the first worksheet

                            // Check if the worksheet is empty
                            if (worksheet.Dimension == null)
                            {
                                System.IO.File.Delete(finalPath);
                                ViewBag.Message = "*Excel file is empty.";
                                return View("Index", _context.FileUploadModal.OrderByDescending(f => f.UploadedOn).ToList());
                            }
                        }
                        // Compress the file for thumbnails (no change in logic)
                        if (fileExt != ".xls" && fileExt != ".xlsx")
                        {
                            if (fileExt == ".svg" || fileExt == ".webp")
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
                        //return Ok("File uploaded and rows saved successfully.");
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
                        if (fileExt == ".xls" || fileExt == ".xlsx")
                        {

                            using (var stream = new MemoryStream())
                            {
                                await file.CopyToAsync(stream);
                                stream.Position = 0;

                                var rows = ProcessExcelFile(stream, fileExt, fileUploadModal.Id);

                                // Save rows to the database
                                _context.RowsData.AddRange(rows);
                                await _context.SaveChangesAsync();
                            }

                        }
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

        private List<RowsData> ProcessExcelFile(Stream fileStream, string fileExt, int fileid)
        {
            var rows = new List<RowsData>();

            IWorkbook workbook;
            if (fileExt == ".xls")
            {
                workbook = new HSSFWorkbook(fileStream); // .xls format
            }
            else
            {
                workbook = new XSSFWorkbook(fileStream); // .xlsx format
            }

            var sheet = workbook.GetSheetAt(0); // Get the first sheet

            for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++) // Assuming the first row is the header
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var row = sheet.GetRow(rowIndex);
                if (row != null)
                {
                    rows.Add(new RowsData
                    {
                        UserId = userId,
                        FileId = fileid,
                        Column1 = row.GetCell(0)?.ToString(),
                        Column2 = row.GetCell(1)?.ToString(),
                        Column3 = row.GetCell(2)?.ToString(),
                        Column4 = row.GetCell(3)?.ToString(),
                        Column5 = row.GetCell(4)?.ToString(),
                        Column6 = row.GetCell(5)?.ToString()
                    });
                }
            }

            return rows;
        }
        public List<Dictionary<string, string>> PreviewExcel(int fileId)
        {
            // Validate inputs
            if (fileId <= 0)
                throw new ArgumentException("Invalid file ID.");

            //if (string.IsNullOrWhiteSpace(userId))
            //    throw new ArgumentException("Invalid user ID.");

            var rows = _context.RowsData
                .Where(r => r.FileId == fileId)
                .OrderBy(o => o.Id)
                .ToList();


            if (!rows.Any())
                throw new InvalidOperationException("No data found for the specified file and user.");

            // Convert rows to a list of dictionaries
            var data = new List<Dictionary<string, string>>();
            foreach (var row in rows)
            {
                var rowData = new Dictionary<string, string>
                {
                    { "Column1", row.Column1 },
                    { "Column2", row.Column2 },
                    { "Column3", row.Column3 },
                    { "Column4", row.Column4 },
                    { "Column5", row.Column5 },
                    { "Column6", row.Column6 }
                };

                data.Add(rowData);
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
            //var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get the Excel data
            var excelData = PreviewExcel(fileId);

            // Apply sorting if requested
            if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortDirection))
            {
                try
                {
                    if (sortDirection.ToLower() == "asc")
                    {
                        excelData = excelData.OrderBy(row =>
                        {
                            var value = row[sortColumn];
                            // Handle values with symbols like "$" or non-numeric characters
                            var cleanedValue = CleanNumericValue(value);
                            return decimal.TryParse(cleanedValue, out var numericValue) ? (object)numericValue : value as object;
                        }).ToList();
                        DTO.DirectionValue = "Ascending";
                    }
                    else if (sortDirection.ToLower() == "desc")
                    {
                        excelData = excelData.OrderByDescending(row =>
                        {
                            var value = row[sortColumn];
                            // Handle values with symbols like "$" or non-numeric characters
                            var cleanedValue = CleanNumericValue(value);
                            return decimal.TryParse(cleanedValue, out var numericValue) ? (object)numericValue : value as object;
                        }).ToList();
                        DTO.DirectionValue = "Descending";
                    }
                }
                catch (Exception ex)
                {
                    DTO.ErrorMsg = ex.Message + Environment.NewLine + "Enter the same type of values in a particular column.";
                }
            }

            DTO.SortedColumnName = sortColumn;
            DTO.ExcelValue = excelData;

            // Return the partial view with sorted data
            return PartialView("_PreviewExcel", DTO);
        }

        private string CleanNumericValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            // Remove non-numeric characters except for the decimal point
            return new string(value.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
        }

        [HttpGet]
        public IActionResult SaveUpdatedExcel()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveUpdatedExcel([FromBody] DataRequest data)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity.Name;

            try
            {
                // Retrieve rows from RowsData table
                var rowsData = await _context.RowsData
                    .Where(r => r.FileId == data.Fileid)
                    .ToListAsync();

                var changes = new List<ExcelChanges>();

                // Compare and update RowsData
                for (int i = 0; i < data.UpdatedData.Count; i++)
                {
                    var updatedRow = data.UpdatedData[i];
                    var existingRow = rowsData[i]; // Assuming row index matches database ID.

                    if (existingRow != null)
                    {
                        for (int colIndex = 0; colIndex < updatedRow.Count; colIndex++)
                        {
                            var columnProperty = $"Column{colIndex + 1}";
                            var oldValue = typeof(RowsData).GetProperty(columnProperty)?.GetValue(existingRow)?.ToString();
                            var newValue = updatedRow[colIndex];

                            if (oldValue != newValue)
                            {
                                // Log the change
                                changes.Add(new ExcelChanges
                                {
                                    UserName = userName,
                                    UserID = userId,
                                    FileId = data.Fileid,
                                    FileName = data.FileName,
                                    Row = i + 1,
                                    Column = colIndex + 1,
                                    OldValue = oldValue,
                                    NewValue = newValue,
                                    ChangeDate = DateTime.Now
                                });

                                // Update RowsData
                                typeof(RowsData).GetProperty(columnProperty)?.SetValue(existingRow, newValue);
                            }
                        }
                    }
                }

                // Save changes to RowsData and ExcelChanges table
                await _context.ExcelChanges.AddRangeAsync(changes);
                await _context.SaveChangesAsync();

                // Update the Excel file
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploads", data.FileName);
                UpdateExcelFile(filePath, data.UpdatedData);

                return Ok(new { success = true , message = "Excel File Updated Successfully"});
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        private void UpdateExcelFile(string filePath, List<List<string>> updatedData)
        {
            IWorkbook workbook;
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                if (Path.GetExtension(filePath).ToLower() == ".xls")
                {
                    workbook = new HSSFWorkbook(fileStream); // For .xls
                }
                else
                {
                    workbook = new XSSFWorkbook(fileStream); // For .xlsx
                }
            }

            var sheet = workbook.GetSheetAt(0); // Assuming data is in the first sheet.

            for (int rowIndex = 0; rowIndex < updatedData.Count; rowIndex++)
            {
                var excelRow = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);

                for (int colIndex = 0; colIndex < updatedData[rowIndex].Count; colIndex++)
                {
                    var cell = excelRow.GetCell(colIndex) ?? excelRow.CreateCell(colIndex);
                    cell.SetCellValue(updatedData[rowIndex][colIndex]);
                }
            }

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write))
            {
                workbook.Write(fs);
            }
        }

        private string GetColumnFormat(List<string> columnValues)
        {
            bool allNumeric = columnValues.All(value => decimal.TryParse(value.Replace("$", "").Replace(",", ""), out _));
            bool allStrings = columnValues.All(value => value.All(c => char.IsLetter(c) || c == ' '));
            bool mixedFormat = columnValues.Any(value => value.StartsWith("$")) || columnValues.Any(value => value.Contains("st") || value.Contains("nd") || value.Contains("rd") || value.Contains("th"));

            if (allStrings)
            {
                return "string";
            }
            else if (allNumeric)
            {
                return "numeric";
            }
            else if (mixedFormat)
            {
                return "mixed";
            }
            else
            {
                return "unknown";
            }
        }

        private bool ValidateInputFormat(string input, string columnFormat)
        {
            if (columnFormat == "numeric")
            {
                return decimal.TryParse(input.Replace("$", "").Replace(",", ""), out _);
            }
            else if (columnFormat == "string")
            {
                return input.All(c => char.IsLetter(c) || c == ' ');
            }
            else if (columnFormat == "mixed")
            {
                // Check if the value starts with a dollar sign and is followed by a valid number (e.g., "$1", "$100.50")
                bool isValidCurrency = input.StartsWith("$") && decimal.TryParse(input.Substring(1).Replace(",", ""), out _);

                // Check if the value contains a number followed by a suffix (e.g., "1st", "2nd", "10th")
                bool isValidMixed = System.Text.RegularExpressions.Regex.IsMatch(input, @"^\d+(st|nd|rd|th)$");

                // Only allow if it's a valid currency or a valid mixed format (like "1st", "2nd")
                return isValidCurrency || isValidMixed;
            }

            else
            {
                return false;
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
                    //ExcelData = PreviewExcel(filePath, fileExt),
                    row = new List<int>(),
                    column = new List<int>(),
                    ExcelChangesData = new List<ExcelChanges>(),
                    isDeleted = file.IsDeleted,
                    DeletedBy = file.DeletedBy,
                    deteleTime = file.DeleteTime
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
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var file = await _context.FileUploadModal.FindAsync(id);
            if (file == null)
            {
                return BadRequest("File Not Found");
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
            string finalPath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploads", file.Filename);
            //string finalPath = file.Data;
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
                file.IsDeleted = true;
                if (user.UserName == "admin@abc.com")
                {
                    file.DeletedBy = "Admin";
                }
                else
                {
                    file.DeletedBy = user.UserName;
                }
                file.DeleteTime = DateTime.Now;
                if (file.Extention == ".xls" || file.Extention == ".xlsx")
                {

                    var data = await _context.ExcelChanges.FirstOrDefaultAsync(f => f.FileId == id);
                    if (data != null) data.isDeleted = true;

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
                    .OrderByDescending(f => f.UploadedOn)
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