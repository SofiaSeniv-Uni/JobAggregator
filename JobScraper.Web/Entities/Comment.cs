namespace JobScraper.Web.Entities;

public class Comment
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public Job Job { get; set; } = null!;
    public string Text { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}