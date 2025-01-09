namespace FileUpload.Models
{
    public class DataRequest
    {
        public int Fileid { get; set; }
        public string FileName { get; set; }
        public List<List<string>> UpdatedData { get; set; }
    }

}
