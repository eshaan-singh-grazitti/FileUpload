namespace FileUpload.Models
{
    public class ExcelChangesViewModel
    {
        public List<Dictionary<string, string>> ExcelData { get; set; }
        //public List<ExcelChanges> Changes { get; set; }
        public List<int> row { get; set; } = null!;
        //public int row { get; set; } 
        public List<int> column { get; set; } = null!;
        //public int column { get; set; }

    }

}
