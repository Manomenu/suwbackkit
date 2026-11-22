using System.Text;
using grate.DependencyInjection;
using grate.Migration;
using grate.postgresql.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Suwbackkit.Postgres;

public static class Migrator
{
    private static readonly long LockKey = BitConverter.ToInt64(Encoding.ASCII.GetBytes("suwmigr!"));

    public static string DefaultDirectory => Path.Combine(AppContext.BaseDirectory, "Database", "Migrations");

    public static Task MigrateAsync(string connectionString, string directory) => RunAsync(connectionString, directory);

    internal static async Task<IReadOnlyList<string>> RunAsync(string connectionString, string directory)
    {
        await using var lockConnection = new NpgsqlConnection(connectionString);
        await lockConnection.OpenAsync().ConfigureAwait(false);
        await LockAsync(lockConnection).ConfigureAwait(false);
        try
        {
            return await RunGrateAsync(connectionString, directory).ConfigureAwait(false);
        }
        finally
        {
            await UnlockAsync(lockConnection).ConfigureAwait(false);
        }
    }

    private static Task LockAsync(NpgsqlConnection connection) =>
        ExecuteAsync(new NpgsqlCommand("SELECT pg_advisory_lock(@key)", connection));

    private static Task UnlockAsync(NpgsqlConnection connection) =>
        ExecuteAsync(new NpgsqlCommand("SELECT pg_advisory_unlock(@key)", connection));

    private static async Task ExecuteAsync(NpgsqlCommand command)
    {
        await using (command.ConfigureAwait(false))
        {
            command.Parameters.AddWithValue("key", LockKey);
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
    }

    private static async Task<IReadOnlyList<string>> RunGrateAsync(string connectionString, string directory)
    {
        var output = Directory.CreateTempSubdirectory("suwbackkit-grate-");
        using var temporaryFolders = new GrateTemporaryFolders(directory);
        IReadOnlyList<string> deleted;
        try
        {
            var services = new ServiceCollection();
            services.AddLogging(logging => logging.AddJsonConsole().AddProvider(temporaryFolders));
            services
                .AddGrate(grate =>
                    grate
                        .WithSqlFilesDirectory(directory)
                        .WithConnectionString(connectionString)
                        .WithOutputFolder(output.FullName)
                        .DoNotCreateDatabase()
                        .WithTransaction()
                )
                .UsePostgreSQL();
            var provider = services.BuildServiceProvider();
            await using (provider.ConfigureAwait(false))
            {
                await provider.GetRequiredService<IGrateMigrator>().Migrate().ConfigureAwait(false);
            }
        }
        finally
        {
            deleted = temporaryFolders.Delete();
            output.Delete(recursive: true);
        }
        return deleted;
    }
}
