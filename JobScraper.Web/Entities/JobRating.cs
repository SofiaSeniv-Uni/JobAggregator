namespace JobScraper.Web.Entities;

public class JobRating
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public Job Job { get; set; } = null!;
    public int Rating { get; set; }
}
