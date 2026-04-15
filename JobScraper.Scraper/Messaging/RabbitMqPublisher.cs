using JobScraper.Scraper.Messaging;
using RabbitMQ.Client;
using System.Text.Json;

public sealed class RabbitMqPublisher : IMessagePublisher
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    private RabbitMqPublisher(IConnection conn, IChannel channel)
        => (_connection, _channel) = (conn, channel);

    public static async Task<RabbitMqPublisher> CreateAsync(string host)
    {
        var factory = new ConnectionFactory { HostName = host };

        IConnection? conn = null;
        var attempts = 0;

        while (conn is null)
        {
            try
            {
                attempts++;
                conn = await factory.CreateConnectionAsync();
            }
            catch (Exception ex) when (attempts < 10)
            {
                Console.WriteLine(
                    $"RabbitMQ not ready, attempt {attempts}/10. " +
                    $"Retrying in 5s... ({ex.Message})");
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
        var channel = await conn.CreateChannelAsync();
        return new RabbitMqPublisher(conn, channel);
    }

    public async Task PublishAsync<T>(T message, string queueName,
        CancellationToken ct = default)
    {
        // Idempotent — ????? ????? ??????????? ?????? ?????
        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,      // ????????? ?????????? RabbitMQ
            exclusive: false,
            autoDelete: false);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            body: body,
            cancellationToken: ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
        await _connection.DisposeAsync();
    }
}