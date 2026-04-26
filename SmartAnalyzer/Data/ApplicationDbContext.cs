using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartAnalyzer.Models;

namespace SmartAnalyzer.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<UploadedFile> UploadedFiles { get; set; }
    public DbSet<FileColumn> FileColumns { get; set; }
    public DbSet<DataRecord> DataRecords { get; set; }
    public DbSet<SavedFilter> SavedFilters { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UploadedFile>()
            .HasOne(f => f.User)
            .WithMany(u => u.UploadedFiles)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<DataRecord>()
            .HasOne(d => d.UploadedFile)
            .WithMany(f => f.DataRecords)
            .HasForeignKey(d => d.UploadedFileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<FileColumn>()
            .HasOne(c => c.UploadedFile)
            .WithMany(f => f.FileColumns)
            .HasForeignKey(c => c.UploadedFileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SavedFilter>()
            .HasOne(s => s.User)
            .WithMany(u => u.SavedFilters)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<SavedFilter>()
            .HasOne(s => s.UploadedFile)
            .WithMany(f => f.SavedFilters)
            .HasForeignKey(s => s.UploadedFileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
