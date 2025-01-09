using System.ComponentModel.DataAnnotations;

namespace FileUpload.Models
{
    public class ExcelChanges
    {
        [Key]
        public int Id {  get; set; }
        public string UserName { get; set; } = null!;
        public string UserID { get; set; } = null!;
        public int  FileId { get; set; }
        public string FileName { get; set; } = null!;
        public int Row { get; set; } 
        public int Column { get; set; }
        public string OldValue { get; set; } = null!;
        public string NewValue { get; set; } = null!;
        public DateTime ChangeDate { get; set; }

    }
}
