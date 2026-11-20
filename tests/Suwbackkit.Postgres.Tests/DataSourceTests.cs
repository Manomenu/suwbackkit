using Npgsql;

namespace Suwbackkit.Postgres.Tests;

public sealed class DataSourceTests(TestDatabase database)
{
    [Fact]
    public async Task A_connection_the_database_killed_while_idle_leaves_the_pool_before_it_is_handed_out()
    {
        var cancellation = TestContext.Current.CancellationToken;
        await using var dataSource = DataSource.Create($"{database.ConnectionString};Keepalive=1");
        int victim;
        await using (var connection = await dataSource.OpenConnectionAsync(cancellation))
        {
            victim = connection.ProcessID;
        }
        Assert.Equal(true, await TestDatabase.ScalarAsync(database.ConnectionString, $"SELECT pg_terminate_backend({victim})"));
        await Task.Delay(TimeSpan.FromSeconds(3), cancellation);

        await using var fresh = await dataSource.OpenConnectionAsync(cancellation);
        await using var probe = new NpgsqlCommand("SELECT 1", fresh);

        Assert.Equal(1, await probe.ExecuteScalarAsync(cancellation));
        Assert.NotEqual(victim, fresh.ProcessID);
    }

    [Fact]
    public async Task The_pool_holds_five_connections_and_sends_keepalives_unless_the_connection_string_says_otherwise()
    {
        await using var defaults = DataSource.Create("Host=db;Username=app");
        await using var configured = DataSource.Create("Host=db;Username=app;Maximum Pool Size=20;Keepalive=0");

        var fromDefaults = new NpgsqlConnectionStringBuilder(defaults.ConnectionString);
        var fromConfiguration = new NpgsqlConnectionStringBuilder(configured.ConnectionString);

        Assert.Equal((DataSource.MaxPoolSize, DataSource.KeepaliveSeconds), (fromDefaults.MaxPoolSize, fromDefaults.KeepAlive));
        Assert.Equal((20, 0), (fromConfiguration.MaxPoolSize, fromConfiguration.KeepAlive));
    }
}
