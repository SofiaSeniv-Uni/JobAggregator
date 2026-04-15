namespace JobScraper.Contracts;

public record JobScrapedEvent(
    string ExternalId,      // ?????????? ID ? ?????
    string Title,
    string Company,
    string? Salary,
    string Location,
    bool IsRemote,
    List<string> Technologies,
    string Url,
    DateTime ScrapedAt,
    string Source            // "DOU" | "Djinni"
);