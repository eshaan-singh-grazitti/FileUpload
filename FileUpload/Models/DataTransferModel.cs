namespace FileUpload.Models
{
    public class DataTransferModel
    {
        public List<Dictionary<string, string>> ExcelValue = null!;
        public string DirectionValue = null!;
        public string SortedColumnName = null!;
        public string ErrorMsg = null!;
    }
}
