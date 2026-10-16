namespace Afc.Api;

public sealed class FootballDataOptions
{
    public const string SectionName = "FootballData";

    public string Provider { get; init; } = "Mock";

    public string BaseUrl { get; init; } = "https://api.football-data.org/v4/";

    public string? ApiKey { get; init; }
}
