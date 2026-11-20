using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Suwbackkit.Postgres;

internal sealed class DatabaseHealthCheck(NpgsqlDataSource dataSource) : IHealthCheck
{
    public const string Name = "database";
    public const string Unreachable = "The database does not answer: start it, or fix ConnectionStrings:Default.";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = dataSource.CreateCommand("SELECT 1");
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (NpgsqlException error)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, Unreachable, error);
        }
    }
}
