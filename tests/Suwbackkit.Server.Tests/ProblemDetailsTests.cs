using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Suwbackkit.Server.Tests;

public sealed class ProblemDetailsTests
{
    [Fact]
    public async Task An_unhandled_error_answers_with_problem_details_whose_trace_id_the_log_names()
    {
        await using var server = await TestApp.StartAsync();

        var response = await server.GetAsync("/api/fail");
        var problem = await ProblemAsync(response);
        var traceId = problem.GetProperty("traceId").GetString();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Matches("^[0-9a-f]{32}$", traceId);
        Assert.Contains(server.Logs.Entries, entry => entry.Level == LogLevel.Error && entry.TraceId == traceId);
    }

    [Fact]
    public async Task An_unknown_route_answers_with_problem_details_and_a_trace_id()
    {
        await using var server = await TestApp.StartAsync();

        var response = await server.GetAsync("/api/nothing-here");
        var problem = await ProblemAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(404, problem.GetProperty("status").GetInt32());
        Assert.Matches("^[0-9a-f]{32}$", problem.GetProperty("traceId").GetString());
    }

    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement.Clone();
}
