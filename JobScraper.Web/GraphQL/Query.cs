using JobScraper.Web.Data;
using JobScraper.Web.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Web.GraphQL;

public class Query
{
    // Отримати всі вакансії з фільтрацією, сортуванням, пагінацією
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Job> GetJobs(AppDbContext db)
        => db.Jobs;

    // Отримати одну вакансію по id
    public async Task<Job?> GetJobById(
        int id, AppDbContext db,
        CancellationToken ct)
        => await db.Jobs
            .Include(j => j.Comments)
            .Include(j => j.Tags)
            .Include(j => j.Rating)
            .FirstOrDefaultAsync(j => j.Id == id, ct);

    // Отримати коментарі до вакансії
    [UseFiltering]
    public IQueryable<Comment> GetComments(
        int jobId, AppDbContext db)
        => db.Comments.Where(c => c.JobId == jobId);
}