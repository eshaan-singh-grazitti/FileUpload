﻿namespace FileUpload.Models
{
    public class ExcelChangesViewModel
    {
        public List<Dictionary<string, string>> ExcelData { get; set; } = null!;
        public List<int> row { get; set; } = null!;
        public List<int> column { get; set; } = null!;
        public List<ExcelAuditTrail> ExcelChangesData { get; set; } = null!;
        public bool isDeleted { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime? deteleTime { get; set; }
    }

}
