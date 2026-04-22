FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY JobScraper.Contracts/ JobScraper.Contracts/
COPY JobScraper.Consumer/  JobScraper.Consumer/

WORKDIR /app/JobScraper.Consumer
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "JobScraper.Consumer.dll"]