namespace Afc.Domain.Tests;

public sealed class FixtureSelectorTests
{
    private static readonly Competition Competition = new("PL", "Premier League", "PL", false);

    [Fact]
    public void SelectNextOrLive_PrefersLiveFixture()
    {
        var fixtures = new[]
        {
            Fixture("next", FixtureStatus.Scheduled, 20),
            Fixture("live", FixtureStatus.Live, -1)
        };

        var result = FixtureSelector.SelectNextOrLive(fixtures, DateTimeOffset.UtcNow);

        Assert.Equal("live", result?.Id);
    }

    [Fact]
    public void SelectNextOrLive_SelectsEarliestScheduledFixture()
    {
        var fixtures = new[]
        {
            Fixture("later", FixtureStatus.Scheduled, 4),
            Fixture("first", FixtureStatus.Scheduled, 1)
        };

        var result = FixtureSelector.SelectNextOrLive(fixtures, DateTimeOffset.UtcNow);

        Assert.Equal("first", result?.Id);
    }

    private static Fixture Fixture(string id, FixtureStatus status, int hoursFromNow) =>
        new(id, Competition, "Arsenal", "Opponent", DateTimeOffset.UtcNow.AddHours(hoursFromNow), status);
}
