using JobScraper.Contracts;

namespace JobScraper.Scraper.Scrapers;

public interface IJobScraper
{
    string Source { get; }
    IAsyncEnumerable<JobScrapedEvent> ScrapeAsync(
        CancellationToken cancellationToken = default);
}