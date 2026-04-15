using Microsoft.Playwright;
using System.Runtime.CompilerServices;
using JobScraper.Contracts;

namespace JobScraper.Scraper.Scrapers;

public sealed class DouJobScraper : PlaywrightScraperBase
{
    private readonly ILogger<DouJobScraper> _logger;
    private const string BaseUrl =
        "https://jobs.dou.ua/vacancies/?category=.NET";

    public DouJobScraper(ILogger<DouJobScraper> logger)
        => _logger = logger;

    public override string Source => "DOU";

    public override async IAsyncEnumerable<JobScrapedEvent> ScrapeAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await InitAsync();

        _logger.LogInformation("Navigating to {Url}", BaseUrl);
        await Page.GotoAsync(BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle   // ??????? ?????????? AJAX
        });

        // DOU ??????????? ???????? ??????? "?? ????????" — ????????, ???? ?
        await LoadAllJobsAsync(cancellationToken);

        // ????????? ??? ?????? ????????
        var cards = await Page.QuerySelectorAllAsync("li.l-vacancy");
        _logger.LogInformation("Found {Count} vacancies", cards.Count);

        foreach (var card in cards)
        {
            if (cancellationToken.IsCancellationRequested) yield break;

            var job = await ParseCardAsync(card);
            if (job is not null)
                yield return job;

            // ???????? ???????? ??? ???????? ??????
            await Task.Delay(Random.Shared.Next(200, 500), cancellationToken);
        }
    }

    private async Task LoadAllJobsAsync(CancellationToken ct)
    {
        // ???????? "???????? ??" ???? ?????? ? ?? ????????
        while (!ct.IsCancellationRequested)
        {
            var btn = await Page.QuerySelectorAsync("a.more-btn");
            if (btn is null) break;

            await btn.ClickAsync();
            // Playwright ????-wait: ??????? ???? ???? ???????? ?????????????
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000, ct);   // ??????? ????????
        }
    }

    private async Task<JobScrapedEvent?> ParseCardAsync(IElementHandle card)
    {
        try
        {
            var titleEl = await card.QuerySelectorAsync(".title a");
            var companyEl = await card.QuerySelectorAsync(".company");
            var salaryEl = await card.QuerySelectorAsync(".salary");
            var cityEl = await card.QuerySelectorAsync(".cities");
            var techEls = await card.QuerySelectorAllAsync(".tech-tag");

            if (titleEl is null) return null;

            var title = await titleEl.InnerTextAsync();
            var url = await titleEl.GetAttributeAsync("href") ?? "";
            var company = companyEl is not null
                          ? await companyEl.InnerTextAsync() : "Unknown";
            var salary = salaryEl is not null
                          ? await salaryEl.InnerTextAsync() : null;
            var city = cityEl is not null
                          ? await cityEl.InnerTextAsync() : "Ukraine";

            var techs = new List<string>();
            foreach (var el in techEls)
                techs.Add(await el.InnerTextAsync());

            // ????????? ?????????????? ID ? URL
            var externalId = Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(url)))[..16];

            return new JobScrapedEvent(
                ExternalId: externalId,
                Title: title.Trim(),
                Company: company.Trim(),
                Salary: salary?.Trim(),
                Location: city.Trim(),
                IsRemote: city.Contains("remote",
                                  StringComparison.OrdinalIgnoreCase),
                Technologies: techs,
                Url: url,
                ScrapedAt: DateTime.UtcNow,
                Source: Source
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse vacancy card");
            return null;
        }
    }
}