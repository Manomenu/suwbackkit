using System.Text.Json.Serialization;

namespace Suwbackkit.Server;

public sealed record Refusal<TCode, TMistake>(string Type, string Title, int Status, string Detail, string TraceId, TCode Code)
    where TCode : notnull
    where TMistake : Mistake<TCode>
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<TMistake>? Mistakes { get; init; }
}
