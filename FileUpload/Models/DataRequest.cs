namespace FileUpload.Models
{
    public class DataRequest
    {
        public string FileName { get; set; }
        public List<List<string>> UpdatedData { get; set; }
    }

}
