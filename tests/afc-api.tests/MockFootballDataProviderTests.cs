namespace Afc.Api.Tests;

public sealed class MockFootballDataProviderTests
{
    [Fact]
    public async Task GetFixturesAsync_ReturnsScheduledArsenalFixtures()
    {
        var provider = new MockFootballDataProvider();

        var fixtures = await provider.GetFixturesAsync(CancellationToken.None);

        Assert.NotEmpty(fixtures);
        Assert.All(fixtures, fixture => Assert.Equal("Arsenal", fixture.HomeTeam));
    }

    [Fact]
    public void GetCompetitions_IncludesEliminatedCompetitions()
    {
        var competitions = MockFootballDataProvider.GetCompetitions();

        Assert.Equal(4, competitions.Count);
        Assert.Contains(competitions, competition => competition.IsEliminated);
    }
}
