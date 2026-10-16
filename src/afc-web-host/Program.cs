var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.Use(async (context, next) =>
{
    if (!Path.HasExtension(context.Request.Path))
    {
        context.Request.Path = "/index.html";
    }

    await next();
});
app.MapStaticAssets();

app.Run();
