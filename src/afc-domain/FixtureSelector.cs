namespace Afc.Domain;

public static class FixtureSelector
{
    public static Fixture? SelectNextOrLive(
        IEnumerable<Fixture> fixtures,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(fixtures);

        return fixtures
            .Where(fixture => fixture.Status == FixtureStatus.Live)
            .OrderBy(fixture => fixture.KickoffUtc)
            .FirstOrDefault()
            ?? fixtures
                .Where(fixture => fixture.Status == FixtureStatus.Scheduled && fixture.KickoffUtc >= nowUtc)
                .OrderBy(fixture => fixture.KickoffUtc)
                .FirstOrDefault();
    }
}
