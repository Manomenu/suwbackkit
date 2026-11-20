using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Suwbackkit.Server.Tests;

public sealed class HealthTests
{
    private const string Failure = "The database does not answer on purpose.";

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task A_healthy_server_answers_both_probes(string path)
    {
        await using var server = await TestApp.StartAsync();

        var response = await server.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_failing_check_makes_the_server_unhealthy_but_still_alive()
    {
        await using var server = await TestApp.StartAsync(Failing);

        var health = await server.GetAsync("/health");
        var alive = await server.GetAsync("/alive");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, alive.StatusCode);
    }

    [Fact]
    public async Task The_health_status_answers_ok_when_every_check_passes()
    {
        await using var server = await TestApp.StartAsync();

        var response = await server.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"status":"ok"}""", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task The_health_status_says_which_check_failed_and_names_the_trace_in_the_log()
    {
        await using var server = await TestApp.StartAsync(Failing);

        var response = await server.GetAsync("/api/health");
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
        var traceId = problem.GetProperty("traceId").GetString();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(Failure, problem.GetProperty("detail").GetString());
        Assert.Matches("^[0-9a-f]{32}$", traceId);
        Assert.Contains(server.Logs.Entries, entry => entry.Level >= LogLevel.Warning && entry.TraceId == traceId);
    }

    private static void Failing(Microsoft.AspNetCore.Builder.WebApplicationBuilder builder) =>
        builder.Services.AddHealthChecks().AddCheck("database", () => HealthCheckResult.Unhealthy(Failure));
}
