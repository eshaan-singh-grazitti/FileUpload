using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileUpload.Models
{
    public class ExcelSheetData
    {
        [Key]
        public int Id { get; set; }
        public string Segment { get; set; } = null!;
        public string Country { get; set; } = null!;
        public string Product { get; set; } = null!;
        public string DiscountBand { get; set; } = null!;
        [Column(TypeName = "decimal(31,2)")]
        public decimal UnitsSold { get; set; }
        public int ManufacturingPrice { get; set; }
        public string UserId { get; set; } = null!;
        public int FileId { get; set; }
        [ForeignKey("FileId")]
        public UploadedFileInfo FileUpload { get; set; } = null!;
    }
}
