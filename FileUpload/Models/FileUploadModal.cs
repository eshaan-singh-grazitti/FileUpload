using System.ComponentModel.DataAnnotations;

namespace FileUpload.Models
{
    public class FileUploadModal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string OriginalFilename { get; set; } = null!;

        [Required]
        public string Filename { get; set; } = null!;

        [Required]
        public string FileType { get; set; } = null!;

        [Required]
        public string Data { get; set; } = null!;

        public double FileSize { get; set; }

        [Required]
        public string CompressedPath { get; set; } = null!;

        [Required]
        public string Extention { get; set; } = null!;

        [Required]
        public DateTime UploadedOn { get; set; }

        [Required]
        public string UserId { get; set; }

        // Navigation property for related ExcelChanges
        public ICollection<ExcelChanges> ExcelChanges { get; set; } = new List<ExcelChanges>();
    }
}
