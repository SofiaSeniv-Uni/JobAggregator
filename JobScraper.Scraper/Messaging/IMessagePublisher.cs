namespace JobScraper.Scraper.Messaging;

public interface IMessagePublisher : IAsyncDisposable
{
    Task PublishAsync<T>(T message, string queueName,
        CancellationToken ct = default);
}