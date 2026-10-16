namespace Afc.Domain;

public interface IFootballDataProvider
{
    Task<IReadOnlyList<Fixture>> GetFixturesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Standing>> GetStandingsAsync(
        string competitionId,
        CancellationToken cancellationToken);

    Task<Lineup> GetLineupsAsync(string fixtureId, CancellationToken cancellationToken);

    Task<MatchStats> GetMatchStatsAsync(string fixtureId, CancellationToken cancellationToken);
}
