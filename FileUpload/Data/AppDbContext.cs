using FileUpload.Models;
using Microsoft.EntityFrameworkCore;

namespace FileUpload.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        public DbSet<FileUploadModal> FileUploadModal { get; set; }
    }
}
