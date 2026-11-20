using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Suwbackkit.Postgres;

internal sealed class DatabaseStartup(IOptions<DatabaseOptions> options) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) =>
        Migrator.MigrateAsync(options.Value.ConnectionString, Migrator.DefaultDirectory);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
