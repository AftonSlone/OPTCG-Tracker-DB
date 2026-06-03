using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OPTCG.Tracker.Data.Data;
using OPTCG.Tracker.Importer.Services;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
    ?? throw new InvalidOperationException("ConnectionStrings__Default environment variable must be set.");

var apiBaseUrl = builder.Configuration["Optcg:BaseUrl"]
    ?? Environment.GetEnvironmentVariable("OPTCG_BASE_URL")
    ?? "https://optcgapi.com/";

builder.Services.AddDbContext<TrackerDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddHttpClient<OptcgApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("OPTCG-Tracker-Importer/1.0");
});

builder.Services.AddScoped<CardImportService>();

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    using var scope = host.Services.CreateScope();
    var importer = scope.ServiceProvider.GetRequiredService<CardImportService>();
    await importer.RunAsync(CancellationToken.None);
    return 0;
}
catch (Exception ex)
{
    logger.LogError(ex, "Import failed.");
    return 1;
}
