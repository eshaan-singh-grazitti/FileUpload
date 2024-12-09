using FileUpload.Data;
using FileUpload.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.HSSF.UserModel;
using OfficeOpenXml;
using System.Drawing;
using System.Drawing.Imaging;

namespace FileUpload.Controllers
{
    public class FileUploadController : Controller
    {
        private readonly string[] ext = { ".png", ".gif", ".jpeg", ".bmp", ".webp", ".jpg", ".svg+xml", ".svg", ".xls", ".xlsx", ".zip" };
        private readonly AppDbContext _context;
        DataTransferModel DTO = new DataTransferModel();
        public FileUploadController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var files = await _context.FileUploadModal.ToListAsync();
            return View(files);
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

                    // If all chunks are uploaded, merge them in parallel
                    if (chunkIndex + 1 == totalChunks)
                    {
                        string finalPath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploads", fileName);
                        string globalPath = Path.Combine(Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).FullName).FullName).FullName, "Uploaded Folder", fileName);
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
                        FileInfo fileInfo = new FileInfo(finalPath);
                        double fileSizeInBytes = fileInfo.Length;
                        double fileSizeInKB = fileSizeInBytes / 1024;
                        double fileSize = Math.Round(fileSizeInKB, 2);
                        var fileUploadModal = new FileUploadModal
                        {
                            OriginalFilename = originalFileName + fileExt,
                            Filename = fileName,
                            FileType = file.ContentType,
                            Data = globalPath,
                            FileSize = fileSize,
                            CompressedPath = compressedFilePath,
                            Extention = fileExt,
                            UploadedOn = DateTime.Now
                        };
                        _context.FileUploadModal.Add(fileUploadModal);
                        await _context.SaveChangesAsync();
                        ViewBag.MessageSuccess = "File uploaded successfully.";
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

            return RedirectToAction("Index", _context.FileUploadModal.OrderByDescending(f => f.UploadedOn).ToList());
        }

        //public IActionResult PreviewExcel(string filePath, out List<Dictionary<string, string>> data)
        //{
        //    data = new List<Dictionary<string, string>>();
        //    if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
        //    {
        //        return BadRequest("File not found.");
        //    }

        //    using var package = new ExcelPackage(new FileInfo(filePath));
        //    if (package.Workbook.Worksheets.Count == 0)
        //    {
        //        return BadRequest("No worksheets found in the Excel file.");
        //    }

        //    var worksheet = package.Workbook.Worksheets.First(); // First worksheet
        //    var rowCount = worksheet.Dimension?.Rows ?? 0;
        //    var colCount = worksheet.Dimension?.Columns ?? 0;
        //    var headers = new List<string>();
        //    for (int col = 1; col <= colCount; col++)
        //    {
        //        headers.Add(worksheet.Cells[1, col].Text);
        //    }

        //    for (int row = 1; row <= rowCount; row++)
        //    {
        //        var rowData = new Dictionary<string, string>();
        //        for (int col = 1; col <= colCount; col++)
        //        {
        //            var cellValue = worksheet.Cells[row, col].Text;
        //            rowData[headers[col - 1]] = cellValue;
        //            if (row == 1)
        //            {
        //                rowData[headers[col - 1]] = null;
        //            }
        //        }
        //        data.Add(rowData);
        //    }

        //    return Json(data); // Send JSON data to frontend
        //}



        private List<Dictionary<string, string>> PreviewExcel(string filePath, string fileExt)
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
                    var workbook = new HSSFWorkbook(file);  // Use HSSFWorkbook for .xls files
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






        //public IActionResult LoadExcelPreview(int fileId, string sortColumn, string sortDirection)
        //{
        //    var file = _context.FileUploadModal.Find(fileId);
        //    if (file == null)
        //    {
        //        return BadRequest("File Not Found");
        //    }

        //    string filePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploads", file.Filename);

        //    // Get the Excel data
        //    var excelData = PreviewExcel(filePath, file.Extention);

        //    // Apply sorting
        //    if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortDirection))
        //    {
        //        if (sortDirection.ToLower() == "asc")
        //        {
        //            excelData = excelData.OrderBy(row => row[sortColumn]).ToList();
        //        }
        //        else if (sortDirection.ToLower() == "desc")
        //        {
        //            excelData = excelData.OrderByDescending(row => row[sortColumn]).ToList();
        //        }
        //    }
        //    DTO.ExcelValue=excelData;


        //    return PartialView("_PreviewExcel", DTO);
        //}




        //[Microsoft.AspNetCore.Mvc.Route("/FileUpload/Details/{id}")]
        //public async Task<IActionResult> Details(int id)
        //{
        //    var file = await _context.FileUploadModal.FindAsync(id);
        //    if (file == null)
        //    {
        //        return BadRequest("File Not Found");
        //    }
        //    string fileName = Path.GetFileName(file.Filename);
        //    string fileExt = Path.GetExtension(file.Filename);
        //    string filePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploads", file.Filename);

        //    if (fileExt == ".xls" || fileExt == ".xlsx")
        //    {
        //        PreviewExcel(filePath, out List<Dictionary<string, string>> data);
        //        ViewBag.ExcelData = data;

        //    }
        //    else
        //    {
        //        ViewBag.ImageData = $"/Uploads/{fileName}";
        //    }

        //    return View(file);
        //}



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
                // Set a flag to identify Excel files in the view

                // Optional: Load initial preview (default unsorted)
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploads", file.Filename);
                var excelData = PreviewExcel(filePath, fileExt);
                DTO.ExcelValue = excelData;
            }
            else
            {
                // Handle image preview
                //DTO.ImageValue= $"/Uploads/{fileName}";
            }

            return View(file); // Pass file data to the view
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

        [Microsoft.AspNetCore.Mvc.Route("/")]
        public async Task<IActionResult> Delete(int id)
        {


            var file = await _context.FileUploadModal.FindAsync(id);
            if (file != null)
            {
                string fileExt = file.Extention;
                string filePath = file.Data;
                string UploadFolder = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploads", file.Filename);
                if (fileExt == ".xls" || fileExt == ".xlsx")
                {
                    if (System.IO.File.Exists(filePath) && System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        System.IO.File.Delete(UploadFolder);
                    }
                }
                else
                {
                    string CompressedfilePath = file.CompressedPath;

                    if (System.IO.File.Exists(filePath) && System.IO.File.Exists(CompressedfilePath) && System.IO.File.Exists(UploadFolder))
                    {
                        System.IO.File.Delete(filePath);
                        System.IO.File.Delete(CompressedfilePath);
                        System.IO.File.Delete(UploadFolder);
                    }
                }
                _context.FileUploadModal.Remove(file);
                await _context.SaveChangesAsync();

                ViewBag.MessageSuccess = "File Deleted Successfully";
            }
            return View("Index", _context.FileUploadModal.OrderByDescending(f => f.UploadedOn).ToList());
        }
    }
}