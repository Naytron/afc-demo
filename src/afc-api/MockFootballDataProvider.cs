using Afc.Domain;

namespace Afc.Api;

public sealed class MockFootballDataProvider : IFootballDataProvider
{
    private static readonly Competition PremierLeague = new("PL", "Premier League", "PL", false);
    private static readonly Competition ChampionsLeague = new("CL", "UEFA Champions League", "UCL", false);
    private static readonly Competition FaCup = new("FAC", "FA Cup", "FA Cup", true);
    private static readonly Competition CarabaoCup = new("EFL", "Carabao Cup", "EFL Cup", true);

    public Task<IReadOnlyList<Fixture>> GetFixturesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Fixture> fixtures =
        [
            new("arsenal-city", PremierLeague, "Arsenal", "Manchester City",
                new DateTimeOffset(2026, 10, 18, 16, 30, 0, TimeSpan.Zero), FixtureStatus.Scheduled),
            new("arsenal-madrid", ChampionsLeague, "Arsenal", "Real Madrid",
                new DateTimeOffset(2026, 10, 21, 20, 0, 0, TimeSpan.Zero), FixtureStatus.Scheduled)
        ];

        return Task.FromResult(fixtures);
    }

    public Task<IReadOnlyList<Standing>> GetStandingsAsync(
        string competitionId,
        CancellationToken cancellationToken)
    {
        var competition = GetCompetition(competitionId);
        IReadOnlyList<Standing> standings =
        [
            new(competition, 1, 8, 7, 1, 0, 18, 22),
            new(competition, 2, 8, 6, 1, 1, 12, 19),
            new(competition, 3, 8, 5, 2, 1, 9, 17)
        ];

        return Task.FromResult(standings);
    }

    public Task<Lineup> GetLineupsAsync(string fixtureId, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Lineups require API-Football and are introduced in Stage 4.");

    public Task<MatchStats> GetMatchStatsAsync(string fixtureId, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Match statistics require API-Football and are introduced in Stage 4.");

    public static IReadOnlyList<Competition> GetCompetitions() =>
        [PremierLeague, ChampionsLeague, FaCup, CarabaoCup];

    private static Competition GetCompetition(string competitionId) =>
        GetCompetitions().SingleOrDefault(competition => competition.Id == competitionId)
        ?? throw new KeyNotFoundException($"Competition '{competitionId}' was not found.");
}
