using System.Diagnostics.CodeAnalysis;
using Npgsql;

namespace Suwbackkit.Testing;

public static class TestDatabases
{
    internal const string Marker = "test";

    private const int AdminTimeoutSeconds = 3;

    public static async Task RecreateAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var name = TestNameOf(connectionString);
        await using var connection = await OpenAdminAsync(connectionString, cancellationToken);
        await ExecuteAsync(connection, $"DROP DATABASE IF EXISTS {Quoted(name)} WITH (FORCE)", cancellationToken);
        await ExecuteAsync(connection, $"CREATE DATABASE {Quoted(name)}", cancellationToken);
    }

    public static async Task DropAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var name = TestNameOf(connectionString);
        await using var connection = await OpenAdminAsync(connectionString, cancellationToken);
        await ExecuteAsync(connection, $"DROP DATABASE IF EXISTS {Quoted(name)} WITH (FORCE)", cancellationToken);
    }

    internal static bool IsTestName(string name) =>
        name.Split('_', '-').Any(word => word.Equals(Marker, StringComparison.OrdinalIgnoreCase));

    private static string TestNameOf(string connectionString)
    {
        var name = new NpgsqlConnectionStringBuilder(connectionString).Database ?? "";
        if (!IsTestName(name))
        {
            throw new InvalidOperationException(
                $"Refusing to drop or recreate the database \"{name}\": its name has no word \"{Marker}\" (between _ or -), so it may be "
                    + $"a development or production database. Point the tests at a database named like <app>_{Marker} or "
                    + $"<app>_{Marker}_<checkout>, never at the app's own database."
            );
        }
        return name;
    }

    private static string Quoted(string name) => $"\"{name.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private static async Task<NpgsqlConnection> OpenAdminAsync(string connectionString, CancellationToken cancellationToken)
    {
        var admin = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "postgres",
            Timeout = AdminTimeoutSeconds,
            Pooling = false,
        };
        var connection = new NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    [SuppressMessage(
        "Security",
        "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "DDL takes no parameters; the only value in it is a database name that passed IsTestName, quoted as an identifier."
    )]
    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
