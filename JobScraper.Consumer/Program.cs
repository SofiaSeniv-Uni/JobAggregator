using JobScraper.Consumer;
using JobScraper.Consumer.Data;
using Microsoft.EntityFrameworkCore;

using Serilog;

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((ctx, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console())
    .ConfigureServices((ctx, services) =>
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(
                ctx.Configuration.GetConnectionString("Postgres")));

        services.AddHostedService<ConsumerWorker>();
    })
    .Build();

// Авто-міграція при старті — зручно для Docker
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
