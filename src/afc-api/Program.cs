using Afc.Api;
using Afc.Domain;
using Azure.Identity;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

var appConfigurationEndpoint = builder.Configuration["AppConfiguration:Endpoint"];
if (Uri.TryCreate(appConfigurationEndpoint, UriKind.Absolute, out var endpoint))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
        options.Connect(endpoint, new DefaultAzureCredential())
            .Select("*")
            .UseFeatureFlags());
    builder.Services.AddAzureAppConfiguration();
}

builder.Services.Configure<FootballDataOptions>(
    builder.Configuration.GetSection(FootballDataOptions.SectionName));
builder.Services.AddMemoryCache();
builder.Services.AddFeatureManagement();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var providerName = builder.Configuration.GetValue<string>("FootballData:Provider");
if (string.Equals(providerName, "FootballDataOrg", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<IFootballDataProvider, FootballDataOrgProvider>((services, client) =>
    {
        var options = services.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<FootballDataOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
        client.DefaultRequestHeaders.Add("X-Auth-Token", options.ApiKey);
    });
}
else
{
    builder.Services.AddSingleton<IFootballDataProvider, MockFootballDataProvider>();
}

var app = builder.Build();
if (endpoint is not null)
{
    app.UseAzureAppConfiguration();
}

app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

var api = app.MapGroup("/api");
api.MapGet("/home", async (
    IFootballDataProvider provider,
    IFeatureManager featureManager,
    TimeProvider timeProvider,
    CancellationToken cancellationToken) =>
{
    if (!await featureManager.IsEnabledAsync("trophy-screen"))
    {
        return Results.NotFound();
    }

    var fixtures = await provider.GetFixturesAsync(cancellationToken);
    var competitions = MockFootballDataProvider.GetCompetitions();
    var standings = await Task.WhenAll(competitions.Select(async competition =>
        new CompetitionSnapshot(
            competition,
            await provider.GetStandingsAsync(competition.Id, cancellationToken))));

    return Results.Ok(new HomeResponse(
        FixtureSelector.SelectNextOrLive(fixtures, timeProvider.GetUtcNow()),
        standings,
        await featureManager.IsEnabledAsync("lineup-notification")));
});

api.MapGet("/competitions/{competitionId}", async (
    string competitionId,
    IFootballDataProvider provider,
    CancellationToken cancellationToken) =>
{
    var competition = MockFootballDataProvider.GetCompetitions()
        .SingleOrDefault(item => item.Id == competitionId);
    if (competition is null)
    {
        return Results.NotFound();
    }

    var standings = await provider.GetStandingsAsync(competitionId, cancellationToken);
    return Results.Ok(new CompetitionSnapshot(competition, standings));
});

app.Run();

public partial class Program;
