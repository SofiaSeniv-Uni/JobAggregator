// Program.cs
using JobScraper.Scraper;
using JobScraper.Scraper.Scrapers;
using JobScraper.Scraper.Messaging;
using Serilog;

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((ctx, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console())
    .ConfigureServices((ctx, services) =>
    {
        // ?????????? ??? ???????? — ????? ?????? ?????
        services.AddTransient<IJobScraper, DouJobScraper>();
        services.AddTransient<IJobScraper, DjinniJobScraper>();

        services.AddSingleton<IMessagePublisher>(_ =>
            RabbitMqPublisher.CreateAsync(
                ctx.Configuration["RabbitMQ:Host"] ?? "localhost")
            .GetAwaiter().GetResult());

        services.AddHostedService<ScraperWorker>();
    })
    .Build();

await host.RunAsync();