namespace FileUpload.Models
{
    public class ExcelEditRequestModel
    {
        public int Fileid { get; set; }
        public string FileName { get; set; } = null!;
        public List<List<string>> UpdatedData { get; set; } = null!;
        public string OgFileName { get; set; } = null!;
    }

}
