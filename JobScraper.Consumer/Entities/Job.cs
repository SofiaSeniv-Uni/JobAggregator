namespace JobScraper.Consumer.Entities;

public class Job
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = null!;  // унікальний, без дублів
    public string Title { get; set; } = null!;
    public string Company { get; set; } = null!;
    public string? Salary { get; set; }
    public string Location { get; set; } = null!;
    public bool IsRemote { get; set; }
    public string Technologies { get; set; } = null!;  // JSON масив
    public string Url { get; set; } = null!;
    public string Source { get; set; } = null!;  // "DOU" | "Djinni"
    public DateTime ScrapedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
