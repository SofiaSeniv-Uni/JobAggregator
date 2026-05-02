// Data/AppDbContext.cs
using JobScraper.Web.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User>      Users      => Set<User>();
    public DbSet<Job>       Jobs       => Set<Job>();
    public DbSet<Comment>   Comments   => Set<Comment>();
    public DbSet<TagEntity> Tags       => Set<TagEntity>();
    public DbSet<JobRating> JobRatings => Set<JobRating>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.Username).IsUnique();
        });

        builder.Entity<Job>(e =>
        {
            e.HasIndex(j => j.ExternalId).IsUnique();
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