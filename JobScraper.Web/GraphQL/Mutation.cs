// GraphQL/Mutation.cs

using JobScraper.Web.Data;
using JobScraper.Web.Entities;

namespace JobScraper.Web.GraphQL;

public class Mutation
{
    // Додати коментар до вакансії
    public async Task<Comment> AddComment(
        int jobId, string text,
        AppDbContext db, CancellationToken ct)
    {
        var comment = new Comment
        {
            JobId = jobId,
            Text = text,
            CreatedAt = DateTime.UtcNow
        };

        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);
        return comment;
    }

    // Оновити коментар
    public async Task<Comment?> UpdateComment(
        int id, string text,
        AppDbContext db, CancellationToken ct)
    {
        var comment = await db.Comments.FindAsync(
            new object[] { id }, ct);

        if (comment is null) return null;

        comment.Text = text;
        await db.SaveChangesAsync(ct);
        return comment;
    }

    // Видалити коментар
    public async Task<bool> DeleteComment(
        int id, AppDbContext db, CancellationToken ct)
    {
        var comment = await db.Comments.FindAsync(
            new object[] { id }, ct);

        if (comment is null) return false;

        db.Comments.Remove(comment);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // Поставити рейтинг вакансії
    public async Task<JobRating> RateJob(
        int jobId, int rating,
        AppDbContext db, CancellationToken ct)
    {
        // Рейтинг від 1 до 5
        if (rating is < 1 or > 5)
            throw new ArgumentException("Rating must be between 1 and 5");

        var existing = db.JobRatings
            .FirstOrDefault(r => r.JobId == jobId);

        if (existing is not null)
        {
            existing.Rating = rating;
        }
        else
        {
            existing = new JobRating
            {
                JobId = jobId,
                Rating = rating
            };
            db.JobRatings.Add(existing);
        }

        await db.SaveChangesAsync(ct);
        return existing;
    }

    // Додати тег до вакансії
    public async Task<TagEntity> AddTag(
        int jobId, string name,
        AppDbContext db, CancellationToken ct)
    {
        var tag = db.Tags
            .FirstOrDefault(t => t.Name == name && t.JobId == jobId);

        if (tag is not null) return tag;

        tag = new TagEntity { JobId = jobId, Name = name };
        db.Tags.Add(tag);
        await db.SaveChangesAsync(ct);
        return tag;
    }

    // Видалити тег
    public async Task<bool> DeleteTag(
        int id, AppDbContext db, CancellationToken ct)
    {
        var tag = await db.Tags.FindAsync(new object[] { id }, ct);
        if (tag is null) return false;

        db.Tags.Remove(tag);
        await db.SaveChangesAsync(ct);
        return true;
    }
}