using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OPTCG.Tracker.Data.Data;

/// <summary>
/// Design-time factory so `dotnet ef migrations add` works without the importer
/// or a live database. The connection string here is only used by tooling.
/// </summary>
public class TrackerDbContextFactory : IDesignTimeDbContextFactory<TrackerDbContext>
{
    public TrackerDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? throw new InvalidOperationException("ConnectionStrings__Default environment variable must be set.");

        var options = new DbContextOptionsBuilder<TrackerDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new TrackerDbContext(options);
    }
}
