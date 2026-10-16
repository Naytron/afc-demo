using System.Net;
using System.Net.Http.Json;
using Afc.Domain;
using Afc.Web.Pages;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Afc.Web.Tests;

public sealed class HomeTests : BunitContext
{
    [Fact]
    public void Home_RendersFourTrophiesAndEliminatedState()
    {
        Services.AddScoped(_ => new HttpClient(new StubHandler()) { BaseAddress = new Uri("http://test/") });
        Services.AddScoped<FootballApiClient>();

        var cut = Render<Home>();
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(4, cut.FindAll(".trophy").Count);
            Assert.Equal(2, cut.FindAll(".trophy.eliminated").Count);
            Assert.Contains("Continue with Google", cut.Markup, StringComparison.Ordinal);
        });
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var competitions = new[]
            {
                Snapshot("PL", false),
                Snapshot("CL", false),
                Snapshot("FAC", true),
                Snapshot("EFL", true)
            };
            var response = new HomeResponse(
                new Fixture(
                    "fixture",
                    competitions[0].Competition,
                    "Arsenal",
                    "Manchester City",
                    DateTimeOffset.UtcNow.AddDays(1),
                    FixtureStatus.Scheduled),
                competitions,
                true);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response)
            });
        }

        private static CompetitionSnapshot Snapshot(string id, bool eliminated)
        {
            var competition = new Competition(id, id, id, eliminated);
            return new CompetitionSnapshot(
                competition,
                [new Standing(competition, 1, 8, 7, 1, 0, 18, 22)]);
        }
    }
}
