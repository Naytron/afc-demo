var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
app.MapStaticAssets();

app.Run();
