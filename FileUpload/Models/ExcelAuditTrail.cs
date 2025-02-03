using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileUpload.Models
{
    public class ExcelAuditTrail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(51)]
        public string UserName { get; set; } = null!;

        [Required]
        public string UserID { get; set; } = null!;

        [Required]
        public int FileId { get; set; }
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = null!;

        // Navigation property for the related FileUpload
        [ForeignKey("FileId")]
        public UploadedFileInfo FileUpload { get; set; } = null!;

        [Required]
        public int Row { get; set; }

        [Required]
        public int Column { get; set; }

        [Required]
        public string OldValue { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string NewValue { get; set; } = null!;

        [Required]
        public DateTime ChangeDate { get; set; }

        public bool isDeleted { get; set; }

    }
}
