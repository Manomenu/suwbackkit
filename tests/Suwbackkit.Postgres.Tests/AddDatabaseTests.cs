using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Suwbackkit.Postgres.Tests;

public sealed class AddDatabaseTests(TestDatabase database)
{
    [Fact]
    public async Task The_server_migrates_the_database_when_it_starts()
    {
        var connectionString = await database.CreateAsync();
        using var host = Host(connectionString, migrateOnStart: true);

        await host.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1L, await TestDatabase.ScalarAsync(connectionString, "SELECT count(*) FROM pg_tables WHERE tablename = 'probe'"));
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task A_server_without_a_connection_string_refuses_to_start_and_says_what_to_set()
    {
        using var host = Host(connectionString: null, migrateOnStart: true);

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync(TestContext.Current.CancellationToken));

        Assert.Contains("ConnectionStrings__Default", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Without_migrations_the_server_starts_with_no_database_at_all()
    {
        using var host = Host(connectionString: null, migrateOnStart: false);

        await host.StartAsync(TestContext.Current.CancellationToken);

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task The_health_check_passes_when_the_database_answers()
    {
        using var host = Host(database.ConnectionString, migrateOnStart: false);

        var report = await host.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Healthy, report.Entries[DatabaseHealthCheck.Name].Status);
    }

    [Fact]
    public async Task The_health_check_fails_and_says_what_to_do_when_the_database_does_not_answer()
    {
        var unreachable = new NpgsqlConnectionStringBuilder(database.ConnectionString) { Port = 1, Timeout = 2 }.ConnectionString;
        using var host = Host(unreachable, migrateOnStart: false);

        var report = await host.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync(TestContext.Current.CancellationToken);
        var entry = report.Entries[DatabaseHealthCheck.Name];

        Assert.Equal(HealthStatus.Unhealthy, entry.Status);
        Assert.Equal(DatabaseHealthCheck.Unreachable, entry.Description);
    }

    [Fact]
    public void The_app_gets_the_npgsql_data_source_as_one_shared_instance()
    {
        using var host = Host(database.ConnectionString, migrateOnStart: false);

        Assert.Same(host.Services.GetRequiredService<NpgsqlDataSource>(), host.Services.GetRequiredService<NpgsqlDataSource>());
    }

    private static IHost Host(string? connectionString, bool migrateOnStart)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = connectionString });
        builder.Services.AddDatabase(builder.Configuration, migrateOnStart);
        return builder.Build();
    }
}
