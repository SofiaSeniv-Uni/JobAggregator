namespace JobScraper.Contracts;

public record JobScrapedEvent(
    string ExternalId,
    string Title,
    string Company,
    string? Salary,
    string Location,
    bool IsRemote,
    string Url,
    DateTime ScrapedAt,
    string Source            // "DOU" | "Djinni"
);