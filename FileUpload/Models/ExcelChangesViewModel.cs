namespace FileUpload.Models
{
    public class ExcelChangesViewModel
    {
        public List<Dictionary<string, string>> ExcelData { get; set; }
        //public List<ExcelChanges> Changes { get; set; }
        public int row {  get; set; }
        public int column { get; set; }
    }

}
