using DotNet.Testcontainers.Builders;
using Npgsql;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(Suwbackkit.Testing.Tests.PostgresServer))]

namespace Suwbackkit.Testing.Tests;

public sealed class PostgresServer : IAsyncLifetime
{
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
            return field ?? throw new InvalidOperationException("the PostgreSQL container is not started");
        }
        private set;
    }

    public async ValueTask InitializeAsync()
    {
        try
        {
            container = new PostgreSqlBuilder("docker.io/library/postgres:17.11").Build();
            await container.StartAsync();
            ConnectionString = container.GetConnectionString();
        }
        catch (DockerUnavailableException error) when (Environment.GetEnvironmentVariable("CI") is null)
        {
            unavailable = error.Message;
        }
    }

    public string Named(string database) => new NpgsqlConnectionStringBuilder(ConnectionString) { Database = database }.ConnectionString;

    public async Task<bool> ExistsAsync(string database)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand("SELECT count(*) FROM pg_database WHERE datname = @name", connection);
        command.Parameters.AddWithValue("name", database);
        return (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken) ?? 0L) == 1;
    }

    public async ValueTask DisposeAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }
}
