# Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY JobScraper.Contracts/ JobScraper.Contracts/
COPY JobScraper.Scraper/   JobScraper.Scraper/

WORKDIR /app/JobScraper.Scraper
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/playwright/dotnet:v1.59.0-jammy AS runtime
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "JobScraper.Scraper.dll"]