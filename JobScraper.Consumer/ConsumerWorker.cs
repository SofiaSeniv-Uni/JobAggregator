using System.Text;
using System.Text.Json;
using JobScraper.Consumer.Data;
using JobScraper.Consumer.Entities;
using JobScraper.Contracts;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace JobScraper.Consumer;

public sealed class ConsumerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<ConsumerWorker> _logger;
    private const string QueueName = "jobs.scraped";

    public ConsumerWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<ConsumerWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IConnection? connection = null;
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMQ:Host"] ?? "rabbitmq"
        };

        for (var attempt = 1; attempt <= 10 && connection is null; attempt++)
        {
            try
            {
                connection = await factory.CreateConnectionAsync(stoppingToken);
            }
            catch (Exception ex) when (attempt < 10)
            {
                _logger.LogWarning(
                    "RabbitMQ not ready, attempt {A}/10. Retrying in 5s... ({M})",
                    attempt, ex.Message);
                await Task.Delay(5000, stoppingToken);
            }
        }

        if (connection is null)
        {
            _logger.LogError("Could not connect to RabbitMQ after 10 attempts");
            return;
        }

        var channel = await connection.CreateChannelAsync(
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);
        
        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                var evt = JsonSerializer.Deserialize<JobScrapedEvent>(body);
                if (evt is null) throw new InvalidOperationException("Null event");

                await SaveJobAsync(evt, stoppingToken);
                
                await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);

                _logger.LogInformation(
                    "Saved: {Title} @ {Company} [{Source}]",
                    evt.Title, evt.Company, evt.Source);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message: {Body}", body);
                
                await channel.BasicNackAsync(
                    ea.DeliveryTag, false, requeue: true,
                    cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Consumer started, waiting for messages...");
        
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task SaveJobAsync(
        JobScrapedEvent evt, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var exists = await db.Jobs
            .AnyAsync(j => j.ExternalId == evt.ExternalId, ct);

        if (exists)
        {
            _logger.LogDebug(
                "Skipping duplicate: {ExternalId}", evt.ExternalId);
            return;
        }

        db.Jobs.Add(new Job
        {
            ExternalId = evt.ExternalId,
            Title = evt.Title,
            Company = evt.Company,
            Salary = evt.Salary,
            Location = evt.Location,
            IsRemote = evt.IsRemote,
            Url = evt.Url,
            Source = evt.Source,
            ScrapedAt = evt.ScrapedAt
        });

        await db.SaveChangesAsync(ct);
    }
}