using JobScraper.GraphQL.Data;
using JobScraper.GraphQL.Entities;

namespace JobScraper.GraphQL;

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
        => await db.Jobs.FindAsync(new object[] { id }, ct);

    // Отримати коментарі до вакансії
    [UseFiltering]
    public IQueryable<Comment> GetComments(
        int jobId, AppDbContext db)
        => db.Comments.Where(c => c.JobId == jobId);
}