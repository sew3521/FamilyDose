using FamilyDose.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FamilyDose.Tests.Infrastructure;

/// <summary>
/// Runs the actual API against a temporary SQLite database.
/// Each factory owns its database and cleans it up afterward.
/// </summary>
public sealed class FamilyDoseApiFactory
    : WebApplicationFactory<Program>
{
    private readonly string _databaseDirectory = Path.Combine(
        Path.GetTempPath(),
        "FamilyDoseTests",
        Guid.NewGuid().ToString("N"));

    // A fixed date keeps tests independent of the actual clock.
    public DateTimeOffset TestNow { get; set; } =
        new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    private string DatabasePath =>
        Path.Combine(_databaseDirectory, "familydose.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the production database configuration.
            services.RemoveAll<DbContextOptions<FamilyDoseDbContext>>();

            // Replace the system clock with a predictable test clock.
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(
                new FixedTimeProvider(TestNow));

            // Each test factory receives its own SQLite database.
            services.AddDbContext<FamilyDoseDbContext>(options =>
                options.UseSqlite($"Data Source={DatabasePath}"));
        });
    }

    /// <summary>
    /// Starts the test server and applies the real EF Core migrations.
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        Directory.CreateDirectory(_databaseDirectory);

        // Access services from the actual test host.
        using var scope = Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<FamilyDoseDbContext>();

        await db.Database.MigrateAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && Directory.Exists(_databaseDirectory))
        {
            Directory.Delete(_databaseDirectory, recursive: true);
        }
    }
}

/// <summary>
/// A clock that always returns the same instant.
/// Used to test date-dependent behavior without waiting for time to pass.
/// </summary>
internal sealed class FixedTimeProvider(DateTimeOffset now)
    : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
