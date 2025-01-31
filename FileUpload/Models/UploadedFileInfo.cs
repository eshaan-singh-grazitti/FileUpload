using System.ComponentModel.DataAnnotations;

namespace FileUpload.Models
{
    public class UploadedFileInfo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; } = null!;

        [Required]
        public string FilenameWithTimeStamp { get; set; } = null!;

        [Required]
        public string FileType { get; set; } = null!;

        [Required]
        public string FilePathOutsideProject { get; set; } = null!;

        public double FileSize { get; set; }

        [Required]
        public string CompressedPath { get; set; } = null!;

        [Required]
        public string Extention { get; set; } = null!;

        [Required]
        public DateTime UploadedOn { get; set; }

        [Required]
        public string UserId { get; set; } = null!;
        public bool IsDeleted { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime? DeleteTime { get; set; }
        // Navigation property for related ExcelChanges
        public ICollection<ExcelAuditTrail> ExcelChanges { get; set; } = new List<ExcelAuditTrail>();
    }
}
