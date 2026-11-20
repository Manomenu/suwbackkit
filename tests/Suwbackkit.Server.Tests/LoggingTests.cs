using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Suwbackkit.Server.Tests;

public sealed class LoggingTests
{
    [Fact]
    public async Task The_console_logs_json_with_scopes_and_utc_timestamps()
    {
        await using var server = await TestApp.StartAsync();

        var console = server.Services.GetRequiredService<IOptionsMonitor<ConsoleLoggerOptions>>().CurrentValue;
        var json = server.Services.GetRequiredService<IOptionsMonitor<JsonConsoleFormatterOptions>>().CurrentValue;

        Assert.Equal(ConsoleFormatterNames.Json, console.FormatterName);
        Assert.True(json.IncludeScopes);
        Assert.True(json.UseUtcTimestamp);
        Assert.Equal("yyyy-MM-ddTHH:mm:ss.fffZ", json.TimestampFormat);
    }

    [Fact]
    public async Task The_app_configuration_overrides_the_logging_defaults()
    {
        await using var server = await TestApp.StartAsync(builder =>
            builder.Configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Logging:Console:FormatterName"] = ConsoleFormatterNames.Simple }
            )
        );

        var console = server.Services.GetRequiredService<IOptionsMonitor<ConsoleLoggerOptions>>().CurrentValue;

        Assert.Equal(ConsoleFormatterNames.Simple, console.FormatterName);
    }
}
