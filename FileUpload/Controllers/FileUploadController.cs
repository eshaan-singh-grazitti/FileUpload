using FileUpload.Data;
using FileUpload.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using OfficeOpenXml;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.IdentityModel.Tokens.Jwt;
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
        [Authorize]
        public async Task<IActionResult> Index()
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            var handler = new JwtSecurityTokenHandler();
            try
            {
                var token = Request.Cookies["JwtToken"];
                if (string.IsNullOrEmpty(token))
                {
                    RedirectToAction("Logout", "Account");
                }
                var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
                var userId = jsonToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Logout", "Account");
                }
            }
            catch (Exception)
            {
                return RedirectToAction("Logout", "Account");
            }

            TempData["message"] = ViewBag.Message;
            var files = await _context.UploadedFileInfo.ToListAsync();
            return View(files);
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UploadedFiles()
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Logout", "Account");
            }
            var handler = new JwtSecurityTokenHandler();
            try
            {
                var token = Request.Cookies["JwtToken"];
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToAction("Logout", "Account");
                }
                var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
                var userId = jsonToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();  // Handle invalid token
                }
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return RedirectToAction("Logout", "Account");
                }
                else
                {
                    bool isInRole = await _userManager.IsInRoleAsync(user, "User");
                    if (isInRole)
                    {
                        var userFiles = await _context.UploadedFileInfo
                            .Where(f => f.UserId == userId)
                            .Where(f => f.IsDeleted == false)
                            .OrderByDescending(f => f.UploadedOn)
                            .ToListAsync();

                        return View(userFiles);
                    }
                    else
                    {
                        return View(_context.UploadedFileInfo.OrderByDescending(f => f.UploadedOn).ToList());
                    }
                }

            }
            catch (Exception)
            {
                return Unauthorized();  // Handle any error in token validation
            }
        }


        [Authorize]
        [HttpPost]
        [Microsoft.AspNetCore.Mvc.Route("/FileUpload/Index")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Upload(IFormFile file, int chunkIndex = 0, int totalChunks = 1)
        {
            var handler = new JwtSecurityTokenHandler();
            try
            {
                var token = Request.Cookies["JwtToken"];
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToAction("Logout", "Account");
                }

                var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
                var userId = jsonToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Logout", "Account");
                }

                try
                {
                    if (file != null)
                    {
                        string fileExt = Path.GetExtension(file.FileName).ToLower();

                        if (!System.Array.Exists(ext, e => e == fileExt))
                        {
                            ViewBag.Message = $"*Invalid file extension: {fileExt}. Allowed types are {string.Join(", ", ext)}.";
                            return View("Index", _context.UploadedFileInfo.OrderByDescending(f => f.UploadedOn).ToList());
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

                        string? baseDirectory = Directory.GetCurrentDirectory();
                        string? parent1 = Directory.GetParent(baseDirectory)?.FullName;
                        string? parent2 = Directory.GetParent(parent1 ?? "")?.FullName;
                        string? parent3 = Directory.GetParent(parent2 ?? "")?.FullName;

                        if (parent3 == null)
                        {
                            throw new InvalidOperationException("Cannot determine the base directory.");
                        }

                        string globalPath = Path.Combine(parent3, "Uploaded Folder", fileName);

                        //string globalPath = Path.Combine(Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).FullName).FullName).FullName, "Uploaded Folder", fileName);
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
                                        return View("Index", _context.UploadedFileInfo.OrderByDescending(f => f.UploadedOn).ToList());
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
                                    return View("Index", _context.UploadedFileInfo.OrderByDescending(f => f.UploadedOn).ToList());
                                }
                            }
                            // Compress the file for thumbnails (no change in logic)
                            if (fileExt != ".xls" && fileExt != ".xlsx" && fileExt != ".zip")
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
                                    using (var image = Image.Load(inputStream))
                                    {
                                        var encoder = new JpegEncoder { Quality = 1 }; // Adjust quality (1-100)

                                        using (var outputStream = new FileStream(compressedFilePath, FileMode.Create))
                                        {
                                            image.Save(outputStream, encoder);
                                        }
                                    }
                                }
                            }
                            //return Ok("File uploaded and rows saved successfully.");
                            FileInfo fileInfo = new FileInfo(finalPath);
                            double fileSizeInBytes = fileInfo.Length;
                            double fileSizeInKB = fileSizeInBytes / 1024;
                            double fileSize = Math.Round(fileSizeInKB, 2);
                            if (string.IsNullOrEmpty(userId))
                            {
                                return BadRequest("User ID not found."); // Handle as needed
                            }
                            var fileUploadModal = new UploadedFileInfo
                            {
                                FileName = originalFileName + fileExt,
                                FilenameWithTimeStamp = fileName,
                                FileType = file.ContentType,
                                FilePathOutsideProject = globalPath,
                                FileSize = fileSize,
                                CompressedPath = compressedFilePath,
                                Extention = fileExt,
                                UploadedOn = DateTime.Now,
                                UserId = userId,
                                IsDeleted = false
                            };
                            _context.UploadedFileInfo.Add(fileUploadModal);
                            await _context.SaveChangesAsync();
                            if (fileExt == ".xls" || fileExt == ".xlsx")
                            {

                                using (var stream = new MemoryStream())
                                {
                                    await file.CopyToAsync(stream);
                                    stream.Position = 0;

                                    var rows = ProcessExcelFile(stream, fileExt, fileUploadModal.Id);

                                    // Save rows to the database
                                    _context.ExcelSheetData.AddRange(rows);
                                    await _context.SaveChangesAsync();
                                }

                            }
                            ViewBag.MessageSuccess = "File uploaded successfully";
                        }
                        else
                        {
                            ViewBag.MessageSuccess = $"Chunk {chunkIndex + 1}/{totalChunks} uploaded successfully.";
                        }

                        return View("Index", _context.UploadedFileInfo.OrderByDescending(f => f.UploadedOn).ToList());
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
            }
            catch (Exception)
            {
                return RedirectToAction("Logout", "Account");
            }

            return RedirectToAction("Index");
        }

        private List<ExcelSheetData> ProcessExcelFile(Stream fileStream, string fileExt, int fileid)
        {
            var rows = new List<ExcelSheetData>();

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
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                var row = sheet.GetRow(rowIndex);
                if (row != null)
                {
                    rows.Add(new ExcelSheetData
                    {
                        UserId = userId,
                        FileId = fileid,
                        Segment = row.GetCell(0).ToString() ?? string.Empty,
                        Country = row.GetCell(1).ToString() ?? string.Empty,
                        Product = row.GetCell(2).ToString() ?? string.Empty,
                        DiscountBand = row.GetCell(3).ToString() ?? string.Empty,
                        UnitsSold = Convert.ToDecimal(row.GetCell(4).ToString()),
                        ManufacturingPrice = Convert.ToInt32(row.GetCell(5).ToString())
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

            // Fetch rows from the database for the given file ID
            var rows = _context.ExcelSheetData
                .Where(r => r.FileId == fileId)
                .OrderBy(o => o.Id)
                .ToList();

            // Ensure there are rows to process
            if (rows.Count == 0)
                return new List<Dictionary<string, string>>();

            // Use the first row as headers
            var headers = new List<string>
            {
                "RowId",
                "Segment",
                "Country",
                "Product",
                "DiscountBand",
                "UnitsSold",
                "ManufacturingPrice"
            };

            // Process the remaining rows as data
            var data = new List<Dictionary<string, string>>();
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowData = new Dictionary<string, string>
                {

                    { headers[0], row.Id.ToString() },
                    { headers[1], row.Segment },
                    { headers[2], row.Country },
                    { headers[3], row.Product },
                    { headers[4], row.DiscountBand },
                    { headers[5], row.UnitsSold.ToString() },
                    { headers[6], row.ManufacturingPrice.ToString() }
                };
                data.Add(rowData);
            }

            return data;
        }

        [HttpPost]
        public IActionResult LoadExcelPreview(int fileId, string sortColumn, string sortDirection)
        {
            var file = _context.UploadedFileInfo.Find(fileId);
            if (file == null)
            {
                return RedirectToAction("FileNotFound");
            }

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploads", file.FilenameWithTimeStamp);
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

                            if (value == null)
                                return (object)string.Empty; // Handle null values safely

                            string stringValue = value.ToString().Trim();

                            // If the value is purely numeric (integer or decimal), parse and sort numerically
                            if (IsPureInteger(stringValue, out int intValue))
                                return intValue;

                            if (IsPureDecimal(stringValue, out decimal decimalValue))
                                return decimalValue;

                            // If the value contains non-numeric characters, sort it as a string
                            return stringValue;
                        }).ToList();

                        DTO.DirectionValue = "Ascending";
                    }
                    else if (sortDirection.ToLower() == "desc")
                    {
                        excelData = excelData.OrderByDescending(row =>
                        {
                            var value = row[sortColumn];

                            if (value == null)
                                return (object)string.Empty; // Handle null values safely

                            string stringValue = value.ToString().Trim();

                            // If the value is purely numeric (integer or decimal), parse and sort numerically
                            if (IsPureInteger(stringValue, out int intValue))
                                return intValue;

                            if (IsPureDecimal(stringValue, out decimal decimalValue))
                                return decimalValue;

                            // If the value contains non-numeric characters, sort it as a string
                            return stringValue;
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
        private static bool IsPureInteger(string value, out int intValue)
        {
            return int.TryParse(value, out intValue);
        }

        private static bool IsPureDecimal(string value, out decimal decimalValue)
        {
            return decimal.TryParse(value, out decimalValue);
        }

        [HttpGet]
        public IActionResult SaveUpdatedExcel()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveUpdatedExcel([FromBody] ExcelEditRequestModel data)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = Request.Cookies["JwtToken"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Logout", "Account");
            }
            var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
            var userId = jsonToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Logout", "Account");
            }
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID not found."); // Handle as needed
            }
            var userName = User?.Identity?.Name ?? "Unknown User";


            try
            {
                // Retrieve rows from ExcelSheetData table and order by rowid (ascending)
                var rowsData = await _context.ExcelSheetData
                    .Where(r => r.FileId == data.Fileid)
                    .ToListAsync();

                if (!rowsData.Any())
                {
                    return BadRequest(new { success = false, message = "No rows found for the given FileId." });
                }

                var changes = new List<ExcelAuditTrail>();
                bool changeDetected = false;


                // Process UpdatedData
                for (int rowIndex = 1; rowIndex <= data.UpdatedData.Count; rowIndex++)
                {
                    var updatedRow = data.UpdatedData[rowIndex - 1];

                    // Find the corresponding row based on rowid (matching by RowId)
                    var rowId = updatedRow[0]; // Assuming the rowId is the first key (or column) of the updated data
                    var existingRow = rowsData.FirstOrDefault(r => r.Id.ToString() == rowId);

                    if (existingRow == null)
                        continue;
                    var columnMapping = new Dictionary<int, string>
                        {
                            { 1, "Segment" },
                            { 2, "Country" },
                            { 3, "Product" },
                            { 4, "DiscountBand" },
                            { 5, "UnitsSold" },
                            { 6, "ManufacturingPrice" }
                        };
                    for (int colIndex = 1; colIndex < updatedRow.Count; colIndex++)
                    {
                        if (!columnMapping.TryGetValue(colIndex, out var columnProperty))
                            continue;
                        var oldValue = typeof(ExcelSheetData).GetProperty(columnProperty)?.GetValue(existingRow)?.ToString();
                        var newValue = updatedRow.ElementAt(colIndex)?.ToString();
                        if (newValue == null || oldValue == null)
                        {
                            return Ok("Value is null");
                        }
                        if (colIndex == 5) // If it's a numeric column
                        {
                            if (decimal.TryParse(newValue, out decimal numericValue))
                            {
                                numericValue = Math.Round(numericValue, 2, MidpointRounding.AwayFromZero); // Round to 2 decimal places
                                newValue = numericValue.ToString("F2"); // Format as string with 2 decimal places

                            }
                            else
                            {
                                return Ok(new { success = false, message = "Invalid numeric value." });
                            }
                        }

                        if (oldValue != newValue)
                        {
                            if (newValue.Length > 50)
                            {
                                return Ok(new { success = false, message = "Maximum of 50 characters can be entered." });
                            }



                            if (!IsValidChangeFormat(oldValue, newValue))
                            {
                                return Ok(new { success = false, message = "Invalid format in new value." });
                            }
                            changeDetected = true;
                            // Log the change
                            changes.Add(new ExcelAuditTrail
                            {
                                UserName = userName,
                                UserID = userId,
                                FileId = data.Fileid,
                                FileName = data.FileName,
                                Row = Convert.ToInt32(rowId), // Assuming rows start from 1
                                Column = colIndex, // Assuming columns start from 1
                                OldValue = oldValue,
                                NewValue = newValue,
                                ChangeDate = DateTime.Now
                            });
                            if (colIndex == 5)
                            {
                                decimal newValueData = Convert.ToDecimal(newValue);
                                typeof(ExcelSheetData).GetProperty(columnProperty)?.SetValue(existingRow, newValueData);
                            }
                            else if (colIndex == 6)
                            {
                                int newValueData = Convert.ToInt32(newValue);
                                typeof(ExcelSheetData).GetProperty(columnProperty)?.SetValue(existingRow, newValueData);
                            }
                            else
                            {
                                // Update RowsData with new value
                                typeof(ExcelSheetData).GetProperty(columnProperty)?.SetValue(existingRow, newValue);
                            }
                        }
                    }
                }


                // Save changes to the database
                await _context.ExcelAuditTrail.AddRangeAsync(changes);
                await _context.SaveChangesAsync();

                // Sort the updated rows based on RowId in ascending order

                if (changeDetected)
                {
                    rowsData = rowsData.OrderBy(r => r.Id).ToList();

                    // Update the Excel file with sorted data
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploads", data.FileName);
                    UpdateExcelFile(filePath, rowsData);
                    return Ok(new { success = true, message = "Excel File Updated Successfully", data = DTO });
                }
                return Ok(new { success = true, message = "No changes Detected", data = DTO });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        private void UpdateExcelFile(string filePath, List<ExcelSheetData> updatedData)
        {
            IWorkbook workbook;

            // Open the Excel file
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (Path.GetExtension(filePath).ToLower() == ".xls")
                {
                    workbook = new HSSFWorkbook(fileStream); // For .xls files
                }
                else
                {
                    workbook = new XSSFWorkbook(fileStream); // For .xlsx files
                }
            } // Ensure file stream is closed before writing

            var sheet = workbook.GetSheetAt(0); // Assuming data is in the first sheet.

            var columnMapping = new Dictionary<int, string>
    {
        { 1, "Segment" },
        { 2, "Country" },
        { 3, "Product" },
        { 4, "DiscountBand" },
        { 5, "UnitsSold" },
        { 6, "ManufacturingPrice" }
    };

            // Start from row index 1 (skip the first row to preserve headers)
            for (int rowIndex = 1; rowIndex <= updatedData.Count; rowIndex++)
            {
                var excelRow = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
                var updatedRow = updatedData[rowIndex - 1]; // Get corresponding row data

                for (int colIndex = 1; colIndex <= columnMapping.Count; colIndex++)
                {
                    if (!columnMapping.TryGetValue(colIndex, out var columnProperty))
                        continue;

                    var columnValue = typeof(ExcelSheetData).GetProperty(columnProperty)?.GetValue(updatedRow)?.ToString();

                    var cell = excelRow.GetCell(colIndex - 1) ?? excelRow.CreateCell(colIndex - 1); // Ensure cell exists
                    cell.SetCellValue(columnValue ?? ""); // Avoid null values
                }
            }

            // Write changes to the file
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                workbook.Write(fs);
            }

            workbook.Close(); // Properly close workbook
        }


        private bool IsValidChangeFormat(string oldValue, string newValue)
        {
            // Check if the old value is numeric (allowing $ at the start and a single dot)
            bool isOldValueNumeric = IsNumeric(oldValue);

            if (isOldValueNumeric)
            {
                // Ensure new value is also numeric (allowing $ at the start and a single dot)
                return IsNumeric(newValue);
            }
            else
            {
                // Old value contains alphabets, new value can contain alphanumeric
                return true; // Allow alphanumeric change
            }
        }

        private bool IsNumeric(string value)
        {
            // Check if the value is numeric, handling $ at the start and a single dot
            if (string.IsNullOrEmpty(value)) return false;

            // Remove $ and check if it's numeric
            string numericValue = value.Trim();

            if (numericValue.StartsWith("$"))
            {
                numericValue = numericValue.Substring(1);
            }

            // Allow only one dot in numeric value
            if (numericValue.Count(c => c == '.') > 1)
            {
                return false;
            }

            return double.TryParse(numericValue, out _);
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Changes(int Id)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = Request.Cookies["JwtToken"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Logout", "Account");
            }
            var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
            var userId = jsonToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Logout", "Account");
            }
            var file = _context.UploadedFileInfo.Find(Id);
            if (file == null)
            {
                return RedirectToAction("FileNotFound");
            }
            var file1 = _context.ExcelAuditTrail
                .Where(f => f.FileId == Id)
                .GroupBy(f => new { f.FileName, f.UserID })
                .Select(g => g
                .OrderBy(f => f.Id)
                .ToList())
                .FirstOrDefault();
            var rowIds = _context.ExcelSheetData
                     .Select(r => r.Id)
                     .ToList();

            var filename = file.FilenameWithTimeStamp;
            string fileExt = Path.GetExtension(filename).ToLower();
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploads", filename);

            if (file1 != null && file1.Count > 0)
            {
                // Ensure EXM is not null

                EXM = new ExcelChangesViewModel
                {
                    ExcelData = PreviewExcel(file.Id),
                    row = new List<int>(),
                    column = new List<int>(),
                    ExcelChangesData = new List<ExcelAuditTrail>(),
                    isDeleted = file.IsDeleted,
                    DeletedBy = file.DeletedBy,
                    deteleTime = file.DeleteTime
                };

                for (var i = 0; i < rowIds.Count; i++)
                {
                    EXM.row.Add(Convert.ToInt32(rowIds[i]));
                }

                for (var i = 0; i < file1.Count; i++)
                {
                    EXM.column.Add(file1[i].Column);
                    EXM.ExcelChangesData.Add(file1[i]);
                }
            }
            else
            {
                return RedirectToAction("FileNotFound");
            }

            return View(EXM);
        }
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = Request.Cookies["JwtToken"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Logout", "Account");
            }
            var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
            var userId = jsonToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Logout", "Account");
            }

            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Logout", "Account");
            }

            var file = await _context.UploadedFileInfo.FindAsync(id);
            if (file == null)
            {
                return RedirectToAction("FileNotFound");
            }
            var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            if (userId != file.UserId && userRole != "Admin")
            {
                return RedirectToAction("FileNotFound");
            }

            return View(file);
        }
        [Authorize]
        public async Task<IActionResult> Download(int id)
        {

            var handler = new JwtSecurityTokenHandler();
            var token = Request.Cookies["JwtToken"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Logout", "Account");
            }
            var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
            var userId = jsonToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Logout", "Account");
            }
            var file = await _context.UploadedFileInfo.FindAsync(id);

            if (file == null)
            {
                return RedirectToAction("FileNotFound");
            }
            var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            if (userId != file.UserId && userRole != "Admin")
            {
                return RedirectToAction("FileNotFound");
            }
            string finalPath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploads", file.FilenameWithTimeStamp);
            //string finalPath = file.Data;
            var fileBytes = await System.IO.File.ReadAllBytesAsync(finalPath);

            return File(fileBytes, file.FileType, file.FileName);
        }
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {

            var handler = new JwtSecurityTokenHandler();
            var token = Request.Cookies["JwtToken"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Logout", "Account");
            }
            var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
            var userId = jsonToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Logout", "Account");
            }
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return BadRequest("User not found."); // Handle as needed
            }

            var file = await _context.UploadedFileInfo.FindAsync(id);

            if (file != null)
            {
                var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (userId != file.UserId && userRole != "Admin")
                {
                    return RedirectToAction("FileNotFound");
                }
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

                    var data = await _context.ExcelAuditTrail.FirstOrDefaultAsync(f => f.FileId == id);
                    if (data != null) data.isDeleted = true;

                }
                await _context.SaveChangesAsync();

                ViewBag.MessageSuccess = "File Deleted Successfully";
            }
            return RedirectToAction("UploadedFiles");

        }

        public IActionResult FileNotFound()
        {
            return View();
        }
    }
}