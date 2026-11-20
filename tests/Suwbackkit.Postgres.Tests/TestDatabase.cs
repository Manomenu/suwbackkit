using System.Diagnostics.CodeAnalysis;
using DotNet.Testcontainers.Builders;
using Npgsql;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(Suwbackkit.Postgres.Tests.TestDatabase))]

namespace Suwbackkit.Postgres.Tests;

public sealed class TestDatabase : IAsyncLifetime
{
    public const string Image = "docker.io/library/postgres:17.11";

    private PostgreSqlContainer? container;
    private string? unavailable;

    public string ConnectionString
    {
        get
        {
            if (unavailable is not null)
            {
                Assert.Skip(
                    "Docker is not available, so this PostgreSQL test did not run: start Docker (or Podman's socket with DOCKER_HOST "
                        + $"pointing at it) and run the tests again. On CI a missing Docker fails the run. ({unavailable})"
                );
            }
            return field ?? throw new InvalidOperationException("the test database is not started");
        }
        private set;
    }

    public async ValueTask InitializeAsync()
    {
        try
        {
            container = new PostgreSqlBuilder(Image).Build();
            await container.StartAsync();
            ConnectionString = container.GetConnectionString();
        }
        catch (DockerUnavailableException error) when (Environment.GetEnvironmentVariable("CI") is null)
        {
            unavailable = error.Message;
        }
    }

    public async Task<string> CreateAsync()
    {
        var database = new NpgsqlConnectionStringBuilder(ConnectionString) { Database = $"probe_{Guid.NewGuid():N}" };
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var create = Command($"CREATE DATABASE \"{database.Database}\"", connection);
        await create.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        return database.ConnectionString;
    }

    public static async Task<object?> ScalarAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = Command(sql, connection);
        return await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);
    }

    [SuppressMessage(
        "Security",
        "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "A test's own SQL, never input."
    )]
    private static NpgsqlCommand Command(string sql, NpgsqlConnection connection) => new(sql, connection);

    public async ValueTask DisposeAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }
}
