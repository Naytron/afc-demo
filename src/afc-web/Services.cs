using System.Net.Http.Json;
using Afc.Domain;

namespace Afc.Web;

public sealed class FootballApiClient(HttpClient httpClient)
{
    public async Task<HomeResponse> GetHomeAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<HomeResponse>("api/home", cancellationToken)
        ?? throw new InvalidOperationException("The home response was empty.");

    public async Task<CompetitionSnapshot> GetCompetitionAsync(
        string competitionId,
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<CompetitionSnapshot>(
            $"api/competitions/{Uri.EscapeDataString(competitionId)}",
            cancellationToken)
        ?? throw new InvalidOperationException("The competition response was empty.");
}
