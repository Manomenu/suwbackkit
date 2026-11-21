using Microsoft.Extensions.Logging;

namespace Suwbackkit.Testing;

public sealed record CapturedLog(string Category, LogLevel Level, string Message, string? TraceId);
