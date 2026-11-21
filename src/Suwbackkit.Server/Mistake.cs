using System.Text.Json.Serialization;

namespace Suwbackkit.Server;

public record Mistake<TCode>(TCode Code, string Message, string Fix)
    where TCode : notnull
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? At { get; init; }
}
