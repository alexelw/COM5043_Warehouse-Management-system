using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Wms.Infrastructure.Persistence;

public class WmsDbContextFactory : IDesignTimeDbContextFactory<WmsDbContext>
{
  public WmsDbContext CreateDbContext(string[] args)
  {
    var configuration = BuildConfiguration();

    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? Environment.GetEnvironmentVariable("WMS_DB_CONNECTION");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
      connectionString = "server=localhost;port=3306;database=wms_db;user=wms_user;password=wms_pass;";
    }

    var optionsBuilder = new DbContextOptionsBuilder<WmsDbContext>();
    optionsBuilder.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 0)),
        mySqlOptions => mySqlOptions.MigrationsAssembly(typeof(WmsDbContext).Assembly.FullName));

    return new WmsDbContext(optionsBuilder.Options);
  }

  private static IConfigurationRoot BuildConfiguration()
  {
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

    var basePath = ResolveBasePath();

    var builder = new ConfigurationBuilder()
        .SetBasePath(basePath)
        .AddJsonFile("appsettings.json", optional: true);

    if (!string.IsNullOrWhiteSpace(environment))
    {
      builder.AddJsonFile($"appsettings.{environment}.json", optional: true);
    }

    builder.AddEnvironmentVariables();

    return builder.Build();
  }

  private static string ResolveBasePath()
  {
    var currentDirectory = Directory.GetCurrentDirectory();
    var candidates = new[]
    {
            currentDirectory,
            Path.Combine(currentDirectory, "src", "Wms.Api"),
            Path.Combine(currentDirectory, "WMS-API", "src", "Wms.Api"),
        };

    foreach (var candidate in candidates)
    {
      if (File.Exists(Path.Combine(candidate, "appsettings.json")))
      {
        return candidate;
      }
    }

    return currentDirectory;
  }
}
