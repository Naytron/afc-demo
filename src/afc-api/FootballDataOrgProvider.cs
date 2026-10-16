using Afc.Domain;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Afc.Api;

public sealed class FootballDataOrgProvider : IFootballDataProvider
{
    private readonly HttpClient httpClient;
    private readonly IMemoryCache cache;
    private readonly FootballDataOptions options;

    public FootballDataOrgProvider(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<FootballDataOptions> options)
    {
        this.httpClient = httpClient;
        this.cache = cache;
        this.options = options.Value;
    }

    public async Task<IReadOnlyList<Fixture>> GetFixturesAsync(CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync("fixtures", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var response = await httpClient.GetFromJsonAsync<MatchesResponse>(
                "teams/57/matches?status=SCHEDULED,LIVE",
                cancellationToken);

            return (IReadOnlyList<Fixture>)(response?.Matches.Select(ToFixture).ToArray()
                ?? Array.Empty<Fixture>());
        }) ?? Array.Empty<Fixture>();

    public async Task<IReadOnlyList<Standing>> GetStandingsAsync(
        string competitionId,
        CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync($"standings:{competitionId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            var response = await httpClient.GetFromJsonAsync<StandingsResponse>(
                $"competitions/{competitionId}/standings",
                cancellationToken);
            var table = response?.Standings.FirstOrDefault()?.Table ?? [];
            var competition = new Competition(competitionId, competitionId, competitionId, false);

            return (IReadOnlyList<Standing>)table.Select(item => new Standing(
                competition,
                item.Position,
                item.PlayedGames,
                item.Won,
                item.Draw,
                item.Lost,
                item.GoalDifference,
                item.Points)).ToArray();
        }) ?? Array.Empty<Standing>();

    public Task<Lineup> GetLineupsAsync(string fixtureId, CancellationToken cancellationToken) =>
        throw new NotSupportedException("football-data.org does not provide lineups.");

    public Task<MatchStats> GetMatchStatsAsync(string fixtureId, CancellationToken cancellationToken) =>
        throw new NotSupportedException("football-data.org does not provide match statistics.");

    private static Fixture ToFixture(MatchItem item) =>
        new(
            item.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            new Competition(
                item.Competition.Code ?? item.Competition.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                item.Competition.Name,
                item.Competition.Code ?? item.Competition.Name,
                false),
            item.HomeTeam.Name,
            item.AwayTeam.Name,
            item.UtcDate,
            item.Status is "IN_PLAY" or "PAUSED" ? FixtureStatus.Live : FixtureStatus.Scheduled);

    private sealed record MatchesResponse(IReadOnlyList<MatchItem> Matches);
    private sealed record MatchItem(
        int Id,
        CompetitionItem Competition,
        TeamItem HomeTeam,
        TeamItem AwayTeam,
        DateTimeOffset UtcDate,
        string Status);
    private sealed record CompetitionItem(int Id, string Name, string? Code);
    private sealed record TeamItem(string Name);
    private sealed record StandingsResponse(IReadOnlyList<StandingGroup> Standings);
    private sealed record StandingGroup(IReadOnlyList<StandingItem> Table);
    private sealed record StandingItem(
        int Position,
        int PlayedGames,
        int Won,
        int Draw,
        int Lost,
        int GoalDifference,
        int Points);
}
