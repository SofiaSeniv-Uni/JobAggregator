using JobScraper.GraphQL.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.GraphQL.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
    : base(options) { }

    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<TagEntity> Tags => Set<TagEntity>();
    public DbSet<JobRating> JobRatings => Set<JobRating>();

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

            e.HasMany(j => j.Comments)
             .WithOne(c => c.Job)
             .HasForeignKey(c => c.JobId);
            e.HasMany(j => j.Tags)
             .WithOne(t => t.Job)
             .HasForeignKey(t => t.JobId);
            e.HasOne(j => j.Rating)
             .WithOne(r => r.Job)
             .HasForeignKey<JobRating>(r => r.JobId);
        });
    }
}
