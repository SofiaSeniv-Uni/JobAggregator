using JobScraper.Contracts;
using Microsoft.Playwright;

namespace JobScraper.Scraper.Scrapers;

public abstract class PlaywrightScraperBase : IJobScraper, IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    protected IPage Page { get; private set; } = null!;

    public abstract string Source { get; }

    // ????????????? ???????? — ????????? ????? ??????????
    protected async Task InitAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            // ??????? ??? Docker: ??? GPU ?? sandbox
            Args = new[] { "--no-sandbox", "--disable-dev-shm-usage" }
        });

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            // ?????????? ??? ?????????? ???????????
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                        "AppleWebKit/537.36 (KHTML, like Gecko) " +
                        "Chrome/120.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            Locale = "uk-UA"
        });

        Page = await context.NewPageAsync();

        // ???????? ????? ??????? — ????????? ????????
        await Page.RouteAsync("**/*.{png,jpg,gif,svg,woff,woff2}",
            r => r.AbortAsync());
    }

    public abstract IAsyncEnumerable<JobScrapedEvent> ScrapeAsync(
        CancellationToken cancellationToken = default);

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
    }
}