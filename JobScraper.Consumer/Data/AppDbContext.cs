using JobScraper.Consumer.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Consumer.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
    : base(options) { }

    public DbSet<Job> Jobs => Set<Job>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>(e =>
        {
            e.HasKey(j => j.Id);

            // ExternalId унікальний — захист від дублів при повторному скрапінгу
            e.HasIndex(j => j.ExternalId).IsUnique();

            e.Property(j => j.Title).HasMaxLength(500);
            e.Property(j => j.Company).HasMaxLength(300);
            e.Property(j => j.Source).HasMaxLength(50);
        });
    }
}
