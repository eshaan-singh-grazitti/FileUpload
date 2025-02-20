using FileUpload.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FileUpload.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        public DbSet<UploadedFileInfo> UploadedFileInfo { get; set; } = null!;
        public DbSet<ExcelAuditTrail> ExcelAuditTrail { get; set; } = null!;
        public DbSet<ExcelSheetData> ExcelSheetData { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<ExcelSheetData>()
            .Property(e => e.UnitsSold)
            .HasColumnType("decimal(31,2)");

            base.OnModelCreating(builder);
        }
    }
}
