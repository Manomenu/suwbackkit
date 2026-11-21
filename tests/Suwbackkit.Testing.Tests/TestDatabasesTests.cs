using Npgsql;

namespace Suwbackkit.Testing.Tests;

public sealed class TestDatabasesTests(PostgresServer postgres)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("myapp_test")]
    [InlineData("myapp_test_3f2a9c")]
    [InlineData("myapp_test_knowledge")]
    [InlineData("myapp_seed_test")]
    [InlineData("test_kb_migration_0c9e")]
    [InlineData("myapp-test")]
    [InlineData("MYAPP_TEST")]
    public void A_name_with_the_word_test_between_underscores_or_hyphens_is_a_test_database(string name) =>
        Assert.True(TestDatabases.IsTestName(name));

    [Theory]
    [InlineData("myapp")]
    [InlineData("postgres")]
    [InlineData("template1")]
    [InlineData("")]
    [InlineData("contest")]
    [InlineData("latest_prices")]
    [InlineData("myapp_tests")]
    [InlineData("myapp_e2e")]
    public void A_name_without_the_word_test_is_not(string name) => Assert.False(TestDatabases.IsTestName(name));

    [Theory]
    [InlineData("myapp")]
    [InlineData("postgres")]
    [InlineData("latest")]
    public async Task Dropping_or_recreating_a_database_whose_name_does_not_say_test_is_refused_before_connecting(string name)
    {
        var unreachable = $"Host=127.0.0.1;Port=1;Database={name};Username=myapp;Password=myapp;Timeout=1";

        var dropped = await Assert.ThrowsAsync<InvalidOperationException>(() => TestDatabases.DropAsync(unreachable, Cancellation));
        var recreated = await Assert.ThrowsAsync<InvalidOperationException>(() => TestDatabases.RecreateAsync(unreachable, Cancellation));

        Assert.Contains($"\"{name}\"", dropped.Message, StringComparison.Ordinal);
        Assert.Contains("never at the app's own database", dropped.Message, StringComparison.Ordinal);
        Assert.Equal(dropped.Message, recreated.Message);
    }

    [Fact]
    public async Task The_app_database_beside_a_test_database_survives_the_refused_calls()
    {
        await using (var connection = new NpgsqlConnection(postgres.ConnectionString))
        {
            await connection.OpenAsync(Cancellation);
            await using var create = new NpgsqlCommand("CREATE DATABASE myapp", connection);
            await create.ExecuteNonQueryAsync(Cancellation);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => TestDatabases.DropAsync(postgres.Named("myapp"), Cancellation));
        await Assert.ThrowsAsync<InvalidOperationException>(() => TestDatabases.RecreateAsync(postgres.Named("myapp"), Cancellation));

        Assert.True(await postgres.ExistsAsync("myapp"));
    }

    [Fact]
    public async Task Recreating_gives_an_empty_database_even_while_a_connection_holds_the_old_one()
    {
        var database = postgres.Named("myapp_test_recreate");
        await TestDatabases.RecreateAsync(database, Cancellation);
        await using var holder = new NpgsqlConnection(database);
        await holder.OpenAsync(Cancellation);
        await using (var table = new NpgsqlCommand("CREATE TABLE notes (id int)", holder))
        {
            await table.ExecuteNonQueryAsync(Cancellation);
        }

        await TestDatabases.RecreateAsync(database, Cancellation);

        await using var fresh = new NpgsqlConnection(database);
        await fresh.OpenAsync(Cancellation);
        await using var tables = new NpgsqlCommand("SELECT count(*) FROM pg_tables WHERE tablename = 'notes'", fresh);
        Assert.Equal(0L, await tables.ExecuteScalarAsync(Cancellation));
    }

    [Fact]
    public async Task Dropping_removes_the_database_and_dropping_one_that_is_gone_is_no_error()
    {
        var database = postgres.Named("myapp_test_drop");
        await TestDatabases.RecreateAsync(database, Cancellation);

        await TestDatabases.DropAsync(database, Cancellation);
        await TestDatabases.DropAsync(database, Cancellation);

        Assert.False(await postgres.ExistsAsync("myapp_test_drop"));
    }
}
