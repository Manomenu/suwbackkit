using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Suwbackkit.Testing.Tests;

public sealed class MappedRoutesTests
{
    [Fact]
    public async Task Every_mapped_route_is_listed_by_method_and_pattern_without_its_constraints_in_order()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();
        app.MapGet("/bots/{bot}/facts/{name:minlength(1)}", () => "");
        app.MapMethods("/bots/{bot}", [HttpMethods.Put, HttpMethods.Delete], () => "");
        var api = app.MapGroup("/api");
        api.MapPost("/runs/{id:guid}/cancel", () => "");
        await app.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            ["DELETE /bots/{bot}", "GET /bots/{bot}/facts/{name}", "POST /api/runs/{id}/cancel", "PUT /bots/{bot}"],
            MappedRoutes.Of(app.Services)
        );
    }

    [Theory]
    [InlineData("/runs/{id:guid}", "/runs/{id}")]
    [InlineData("/runs/{id:int:min(1)}/{rest}", "/runs/{id}/{rest}")]
    [InlineData("/files/{**path}", "/files/{**path}")]
    public void A_constraint_is_stripped_and_nothing_else(string pattern, string expected) =>
        Assert.Equal(expected, MappedRoutes.WithoutConstraints(pattern));
}
