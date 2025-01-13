using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileUpload.Models
{
    public class ExcelChanges
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserName { get; set; } = null!;

        [Required]
        public string UserID { get; set; } = null!;

        [Required]
        public int FileId { get; set; }
        [Required]
        public string FileName { get; set; } = null!;

        // Navigation property for the related FileUpload
        [ForeignKey("FileId")]
        public FileUploadModal FileUpload { get; set; } = null!;

        [Required]
        public int Row { get; set; }

        [Required]
        public int Column { get; set; }

        [Required]
        public string OldValue { get; set; } = null!;

        [Required]
        public string NewValue { get; set; } = null!;

        [Required]
        public DateTime ChangeDate { get; set; }
    }
}
