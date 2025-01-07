using System.ComponentModel.DataAnnotations;

namespace FileUpload.Models
{
    public class FileUploadModal
    {
        [Key]
        public int Id { get; set; }
        public string OriginalFilename { get; set; } = null!;
        public string Filename { get; set; } = null!;
        public string FileType { get; set; } = null!;
        public string Data { get; set; } = null!;
        public double FileSize { get; set; }
        public string CompressedPath { get; set; } = null!;
        public string Extention { get; set; } = null!;
        public DateTime UploadedOn { get; set; }
        public string UserId { get; set; }
    }
}
