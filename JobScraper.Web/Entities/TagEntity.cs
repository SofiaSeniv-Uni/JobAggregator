namespace JobScraper.Web.Entities;

public class TagEntity
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public Job Job { get; set; } = null!;
    public string Name { get; set; } = null!;
}
