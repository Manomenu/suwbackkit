using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using CheckStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Suwbackkit.Server;

public static class HealthEndpoints
{
    public static RouteHandlerBuilder MapHealthStatus(this IEndpointRouteBuilder routes) =>
        routes.MapGet("/health", Check).AllowAnonymous();

    private static async Task<Results<Ok<HealthStatus>, ProblemHttpResult>> Check(
        HealthCheckService health,
        CancellationToken cancellationToken
    )
    {
        var report = await health.CheckHealthAsync(cancellationToken);
        if (report.Status != CheckStatus.Unhealthy)
        {
            return TypedResults.Ok(new HealthStatus("ok"));
        }
        var failures = report
            .Entries.Where(entry => entry.Value.Status == CheckStatus.Unhealthy)
            .Select(entry => entry.Value.Description ?? $"The health check {entry.Key} failed.");
        return TypedResults.Problem(detail: string.Join(" ", failures), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
