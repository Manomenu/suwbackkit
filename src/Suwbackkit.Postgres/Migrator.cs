using grate.DependencyInjection;
using grate.Migration;
using grate.postgresql.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Suwbackkit.Postgres;

public static class Migrator
{
    public static string DefaultDirectory => Path.Combine(AppContext.BaseDirectory, "Database", "Migrations");

    public static Task MigrateAsync(string connectionString, string directory) => RunAsync(connectionString, directory);

    internal static async Task<IReadOnlyList<string>> RunAsync(string connectionString, string directory)
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
