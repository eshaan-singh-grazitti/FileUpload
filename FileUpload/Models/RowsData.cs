using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileUpload.Models
{
    public class RowsData
    {
        [Key]
        public int Id { get; set; }
        public int RowId { get; set; }
        public string Segment { get; set; }
        public string Country { get; set; }
        public string Product { get; set; }
        public string DiscountBand { get; set; }
        public decimal UnitsSold { get; set; }
        public int ManufacturingPrice { get; set; }
        public string UserId { get; set; } = null!;
        public int FileId { get; set; }
        [ForeignKey("FileId")]
        public FileUploadModal FileUpload { get; set; } = null!;
    }
}
