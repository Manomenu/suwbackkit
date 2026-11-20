using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Suwbackkit.Server.Tests;

internal sealed class TestApp : IAsyncDisposable
{
    private readonly WebApplication app;

    private TestApp(WebApplication app, CapturedLogs logs)
    {
        this.app = app;
        Logs = logs;
        Client = app.GetTestClient();
    }

    public HttpClient Client { get; }

    public CapturedLogs Logs { get; }

    public IServiceProvider Services => app.Services;

    public Task<HttpResponseMessage> GetAsync(string path) =>
        Client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

    public static async Task<TestApp> StartAsync(Action<WebApplicationBuilder>? configure = null, Action<IEndpointRouteBuilder>? map = null)
    {
        var logs = new CapturedLogs();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.AddServerDefaults();
        builder.Services.AddSingleton<ILoggerProvider>(logs);
        configure?.Invoke(builder);
        var app = builder.Build();
        app.MapServerDefaults();
        app.MapGroup("/api").MapHealthStatus();
        app.MapGet("/api/fail", () => FailOnPurpose());
        map?.Invoke(app);
        app.MapOpenApi();
        await app.StartAsync(TestContext.Current.CancellationToken);
        return new TestApp(app, logs);
    }

    private static string FailOnPurpose() => throw new InvalidOperationException("a failure on purpose");

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await app.DisposeAsync();
    }
}
