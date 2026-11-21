using System.Text.Json;

namespace Suwbackkit.Cli;

public sealed record ServerMistake(string? Code, string? Message, string? Fix, string? At, JsonElement Json);
