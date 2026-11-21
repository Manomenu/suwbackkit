using Microsoft.Extensions.Logging;

namespace Suwbackkit.Testing.Tests;

public sealed partial class CapturedLogsTests
{
    [Fact]
    public void Each_entry_keeps_its_category_level_message_and_the_trace_id_of_the_scope_it_was_written_in()
    {
        using var logs = new CapturedLogs();
        using var factory = LoggerFactory.Create(builder => builder.AddProvider(logs));
        var logger = factory.CreateLogger("Notes");

        BeforeTheRequest(logger);
        using (logger.BeginScope(new Dictionary<string, object> { ["TraceId"] = "4bf92f3577b34da6a3ce929d0e0e4736" }))
        {
            NoteMissing(logger, 7);
        }

        Assert.Equal(
            [
                new CapturedLog("Notes", LogLevel.Information, "Before the request", null),
                new CapturedLog("Notes", LogLevel.Warning, "The note 7 is missing", "4bf92f3577b34da6a3ce929d0e0e4736"),
            ],
            logs.Entries
        );
    }

    [Fact]
    public void The_entries_are_a_copy_that_later_logs_do_not_change()
    {
        using var logs = new CapturedLogs();
        var logger = logs.CreateLogger("Notes");
        First(logger);

        var seen = logs.Entries;
        Second(logger);

        Assert.Single(seen);
        Assert.Equal(2, logs.Entries.Count);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Before the request")]
    private static partial void BeforeTheRequest(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The note {Id} is missing")]
    private static partial void NoteMissing(ILogger logger, int id);

    [LoggerMessage(Level = LogLevel.Information, Message = "first")]
    private static partial void First(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "second")]
    private static partial void Second(ILogger logger);
}
