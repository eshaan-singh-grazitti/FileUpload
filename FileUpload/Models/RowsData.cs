using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileUpload.Models
{
    public class RowsData
    {
        [Key]
        public int Id { get; set; }
        public string Column1 { get; set; } = null!;
        public string Column2 { get; set; } = null!;
        public string Column3 { get; set; } = null!;
        public string Column4 { get; set; } = null!;
        public string Column5 { get; set; } = null!;
        public string Column6 { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public int FileId { get; set; }
        [ForeignKey("FileId")]
        public FileUploadModal FileUpload { get; set; } = null!;
    }
}
