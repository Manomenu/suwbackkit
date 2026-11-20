using Microsoft.Extensions.Logging;

namespace Suwbackkit.Server.Tests;

internal sealed record CapturedLog(string Category, LogLevel Level, string Message, string? TraceId);
