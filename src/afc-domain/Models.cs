namespace Afc.Domain;

public sealed record Competition(
    string Id,
    string Name,
    string ShortName,
    bool IsEliminated);

public sealed record Fixture(
    string Id,
    Competition Competition,
    string HomeTeam,
    string AwayTeam,
    DateTimeOffset KickoffUtc,
    FixtureStatus Status,
    int? HomeScore = null,
    int? AwayScore = null);

public enum FixtureStatus
{
    Scheduled,
    Live,
    Finished
}

public sealed record Standing(
    Competition Competition,
    int Position,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int GoalDifference,
    int Points);

public sealed record CompetitionSnapshot(
    Competition Competition,
    IReadOnlyList<Standing> Standings);

public sealed record HomeResponse(
    Fixture? FeaturedFixture,
    IReadOnlyList<CompetitionSnapshot> Competitions,
    bool IsLineupNotificationEnabled);

public sealed record Lineup(string FixtureId);

public sealed record MatchStats(string FixtureId);
