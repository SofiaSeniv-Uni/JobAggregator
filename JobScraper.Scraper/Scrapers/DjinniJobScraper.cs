using System.Runtime.CompilerServices;
using JobScraper.Contracts;
using Microsoft.Playwright;

namespace JobScraper.Scraper.Scrapers;

public sealed class DjinniJobScraper : PlaywrightScraperBase
{
	private readonly ILogger<DjinniJobScraper> _logger;
	private const string BaseUrl =
		"https://djinni.co/jobs/?primary_keyword=.NET";

	public DjinniJobScraper(ILogger<DjinniJobScraper> logger)
		=> _logger = logger;

	public override string Source => "Djinni";

	public override async IAsyncEnumerable<JobScrapedEvent> ScrapeAsync(
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		await InitAsync();

		_logger.LogInformation("Navigating to {Url}", BaseUrl);
		await Page.GotoAsync(BaseUrl, new PageGotoOptions
		{
			WaitUntil = WaitUntilState.NetworkIdle
		});

		while (!cancellationToken.IsCancellationRequested)
		{
			var cards = await Page.QuerySelectorAllAsync("ul.list-jobs > div.job-item");
			_logger.LogInformation(
				"Page contains {Count} vacancies", cards.Count);

			foreach (var card in cards)
			{
				if (cancellationToken.IsCancellationRequested) yield break;

				var job = await ParseCardAsync(card);
				if (job is not null)
					yield return job;

				await Task.Delay(
					Random.Shared.Next(150, 400), cancellationToken);
			}
			
			var nextBtn = await Page.QuerySelectorAsync(
				"a[rel='next']");
			if (nextBtn is null) break; 

			await nextBtn.ClickAsync();
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Task.Delay(1500, cancellationToken);
		}
	}

	private async Task<JobScrapedEvent?> ParseCardAsync(IElementHandle card)
	{
		try
		{
			var titleEl = await card.QuerySelectorAsync(
				"a.job_item__header-link");
			if (titleEl is null) return null;

			var h2 = await card.QuerySelectorAsync("h2.job-item__position");

			var title = h2 is not null
				? (await h2.InnerTextAsync()).Trim()
				: (await titleEl!.InnerTextAsync()).Trim();
			var relUrl = await titleEl.GetAttributeAsync("href") ?? "";
			var url = relUrl.StartsWith("http")
						   ? relUrl
						   : $"https://djinni.co{relUrl}";
			
			var companyEl = await card.QuerySelectorAsync("span.small.text-gray-800");
			var company = companyEl is not null
				? (await companyEl.InnerTextAsync()).Trim()
				: "Unknown";

			var salaryEl = await card.QuerySelectorAsync(
				"strong.text-success span.text-success");
			var salary = salaryEl is not null
				? (await salaryEl.InnerTextAsync()).Trim()
				: null;

			var locationEl = await card.QuerySelectorAsync("span.location-text");
			var location = locationEl is not null
				? (await locationEl.InnerTextAsync()).Trim()
				: "Ukraine";

			var remoteEl = await card.QuerySelectorAsync(
				"div.fw-medium span.text-nowrap:first-child");
			var remoteText = remoteEl is not null
				? (await remoteEl.InnerTextAsync()).Trim()
				: "";
			var isRemote = remoteText.Contains("віддален", StringComparison.OrdinalIgnoreCase)
			               || location.Contains("remote", StringComparison.OrdinalIgnoreCase);

			var externalId = Convert.ToBase64String(
				System.Security.Cryptography.SHA256.HashData(
					System.Text.Encoding.UTF8.GetBytes(url)))[..16];

			return new JobScrapedEvent(
				ExternalId: externalId,
				Title: title,
				Company: company,
				Salary: salary,
				Location: location,
				IsRemote: isRemote,
				Url: url,
				ScrapedAt: DateTime.UtcNow,
				Source: Source
			);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to parse Djinni card");
			return null;
		}
	}
}