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

		// Djinni ??????? ?????????, ? ?? ?????? "??" — ???????? ????????
		while (!cancellationToken.IsCancellationRequested)
		{
			var cards = await Page.QuerySelectorAllAsync("li.list-jobs__item");
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

			// ??????? ?????? "???????? ????????"
			var nextBtn = await Page.QuerySelectorAsync(
				"a[rel='next']");
			if (nextBtn is null) break;   // ?????? ?? ????????? ????????

			await nextBtn.ClickAsync();
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Task.Delay(1500, cancellationToken);  // ????? ??? ??????????
		}
	}

	private async Task<JobScrapedEvent?> ParseCardAsync(IElementHandle card)
	{
		try
		{
			// ????????? ? ?????????
			var titleEl = await card.QuerySelectorAsync(
				"a.job-item__title-link");
			if (titleEl is null) return null;

			var title = (await titleEl.InnerTextAsync()).Trim();
			var relUrl = await titleEl.GetAttributeAsync("href") ?? "";
			var url = relUrl.StartsWith("http")
						   ? relUrl
						   : $"https://djinni.co{relUrl}";

			// ????????
			var companyEl = await card.QuerySelectorAsync(
				"a.job-list-item__link--logo span");
			var company = companyEl is not null
				? (await companyEl.InnerTextAsync()).Trim()
				: "Unknown";

			// ???????? (???? ?? ????)
			var salaryEl = await card.QuerySelectorAsync(
				"span.public-salary-item");
			var salary = salaryEl is not null
				? (await salaryEl.InnerTextAsync()).Trim()
				: null;

			// ??????? + remote-?????
			var locationEls = await card.QuerySelectorAllAsync(
				"span.location-text, .job-list-item__job-info span");
			var locationParts = new List<string>();
			foreach (var el in locationEls)
				locationParts.Add((await el.InnerTextAsync()).Trim());

			var locationRaw = string.Join(", ", locationParts
				.Where(s => !string.IsNullOrWhiteSpace(s)));
			var isRemote = locationRaw.Contains(
				"remote", StringComparison.OrdinalIgnoreCase)
				|| locationRaw.Contains(
				"?????????", StringComparison.OrdinalIgnoreCase);

			// ?????????? — Djinni ???????? ?? ?? ???? ??? ???????
			var techEls = await card.QuerySelectorAllAsync(
				"span.technologies-item");
			var techs = new List<string>();
			foreach (var el in techEls)
				techs.Add((await el.InnerTextAsync()).Trim());

			var externalId = Convert.ToBase64String(
				System.Security.Cryptography.SHA256.HashData(
					System.Text.Encoding.UTF8.GetBytes(url)))[..16];

			return new JobScrapedEvent(
				ExternalId: externalId,
				Title: title,
				Company: company,
				Salary: salary,
				Location: locationRaw.Length > 0 ? locationRaw : "Ukraine",
				IsRemote: isRemote,
				Technologies: techs,
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