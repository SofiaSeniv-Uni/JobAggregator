using JobScraper.Scraper.Scrapers;
using JobScraper.Scraper.Messaging;

namespace JobScraper.Scraper;

public sealed class ScraperWorker : BackgroundService
{
    private readonly IEnumerable<IJobScraper> _scrapers;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<ScraperWorker> _logger;
    private const string QueueName = "jobs.scraped";

    public ScraperWorker(
        IEnumerable<IJobScraper> scrapers,
        IMessagePublisher publisher,
        ILogger<ScraperWorker> logger)
    {
        _scrapers = scrapers;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScraperWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var scraper in _scrapers)
            {
                _logger.LogInformation("Starting scraper: {Source}", scraper.Source);

                await foreach (var job in scraper.ScrapeAsync(stoppingToken))
                {
                    await _publisher.PublishAsync(job, QueueName, stoppingToken);
                    _logger.LogInformation("Published: {Title} @ {Company}",
                                     job.Title, job.Company);
                }
            }

            // Повторюємо кожні 2 години
            _logger.LogInformation("Scraping cycle done. Next run in 2h");
            await Task.Delay(TimeSpan.FromHours(2), stoppingToken);
        }
    }
}