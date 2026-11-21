using System.Text.Json;

namespace Suwbackkit.Cli;

public sealed record ServerRefusal(string? Code, string? Title, string? Detail, IReadOnlyList<ServerMistake> Mistakes)
{
    public static ServerRefusal? Read(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            return Read(document.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static ServerRefusal? Read(JsonElement body) =>
        body.ValueKind != JsonValueKind.Object
            ? null
            : new ServerRefusal(
                Text(body, "code"),
                Text(body, "title"),
                Text(body, "detail"),
                body.TryGetProperty("mistakes", out var mistakes) && mistakes.ValueKind == JsonValueKind.Array
                    ? [.. mistakes.EnumerateArray().Select(MistakeOf)]
                    : []
            );

    private static ServerMistake MistakeOf(JsonElement mistake) =>
        new(Text(mistake, "code"), Text(mistake, "message"), Text(mistake, "fix"), Text(mistake, "at"), mistake.Clone());

    private static string? Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
