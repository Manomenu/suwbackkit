using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Suwbackkit.Server.Tests;

public sealed class RefusalTests
{
    private const string Problem = "application/problem+json";

    [JsonConverter(typeof(JsonStringEnumConverter<NoteCode>))]
    internal enum NoteCode
    {
        [JsonStringEnumMemberName("note.invalid")]
        Invalid,

        [JsonStringEnumMemberName("note.stale")]
        Stale,

        [JsonStringEnumMemberName("note.titleMissing")]
        TitleMissing,

        [JsonStringEnumMemberName("note.linkBroken")]
        LinkBroken,
    }

    [JsonConverter(typeof(JsonStringEnumConverter<Stage>))]
    internal enum Stage
    {
        Save,
        Push,
    }

    internal sealed record Related(string Kind, string Id);

    internal sealed record NoteMistake(NoteCode Code, string Message, string Fix, Stage Stage, IReadOnlyList<Related> Related)
        : Mistake<NoteCode>(Code, Message, Fix);

    private static readonly NoteMistake[] TwoMistakes =
    [
        new(NoteCode.TitleMissing, "The note has no title.", "Give it a title.", Stage.Save, []) { At = "title" },
        new(
            NoteCode.LinkBroken,
            "The note links to procedure 'refund', which does not exist.",
            "Link an existing procedure.",
            Stage.Push,
            [new("procedure", "refund")]
        ),
    ];

    private static void Map(IEndpointRouteBuilder routes)
    {
        routes
            .MapPost(
                "/api/notes/7",
                () =>
                    Refusals.Refuse(StatusCodes.Status409Conflict, "The note changed", NoteCode.Stale, "Read it again and save once more.")
            )
            .ProducesRefusal<NoteCode>(StatusCodes.Status409Conflict);
        routes
            .MapPut(
                "/api/notes/7",
                () => Refusals.Refuse(StatusCodes.Status422UnprocessableEntity, "The note cannot be saved", NoteCode.Invalid, TwoMistakes)
            )
            .ProducesRefusal<NoteCode, NoteMistake>(StatusCodes.Status422UnprocessableEntity);
        routes
            .MapDelete(
                "/api/notes/7",
                () => Refusals.Refuse(StatusCodes.Status403Forbidden, "Not yours", "note.forbidden", "Ask its owner.")
            )
            .ProducesRefusal<string>(StatusCodes.Status403Forbidden);
    }

    private static async Task<(HttpResponseMessage Response, JsonElement Body)> SendAsync(HttpMethod method, string path = "/api/notes/7")
    {
        await using var server = await TestApp.StartAsync(map: Map);
        using var request = new HttpRequestMessage(method, path);
        var response = await server.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement.Clone();
        return (response, body);
    }

    private static async Task<JsonNode> DocumentAsync()
    {
        await using var server = await TestApp.StartAsync(map: Map);
        var json = await server.Client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative), TestContext.Current.CancellationToken);
        return JsonNode.Parse(json) ?? throw new InvalidOperationException(json);
    }

    private static JsonNode? Resolved(JsonNode document, JsonNode? schema)
    {
        var reference = schema?["$ref"]?.GetValue<string>();
        return reference is null ? schema : document["components"]?["schemas"]?[reference.Split('/')[^1]];
    }

    private static JsonNode? ResponseSchema(JsonNode document, string method, int status) =>
        Resolved(
            document,
            document["paths"]
                ?["/api/notes/7"]
                ?[method]
                ?["responses"]
                ?[status.ToString(System.Globalization.CultureInfo.InvariantCulture)]
                ?["content"]
                ?[Problem]
                ?["schema"]
        );

    private static IEnumerable<string?> Required(JsonNode? schema) =>
        schema?["required"]?.AsArray().Select(name => name?.GetValue<string>()) ?? [];

    private static IEnumerable<string?> EnumOf(JsonNode document, JsonNode? schema) =>
        Resolved(document, schema)?["enum"]?.AsArray().Select(value => value?.GetValue<string>()) ?? [];

    [Fact]
    [Trait("criterion", "AC-refusal-format-1")]
    public async Task A_refusal_is_problem_json_with_its_status_a_title_a_detail_and_a_code()
    {
        var (response, body) = await SendAsync(HttpMethod.Post);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(Problem, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(409, body.GetProperty("status").GetInt32());
        Assert.Equal("The note changed", body.GetProperty("title").GetString());
        Assert.Equal("Read it again and save once more.", body.GetProperty("detail").GetString());
        Assert.Equal("note.stale", body.GetProperty("code").GetString());
        Assert.False(body.TryGetProperty("mistakes", out _));
        Assert.Matches("^[0-9a-f]{32}$", body.GetProperty("traceId").GetString());
    }

    [Theory]
    [Trait("criterion", "AC-refusal-format-1")]
    [InlineData("", "Ask its owner.", "note.forbidden")]
    [InlineData("Not yours", " ", "note.forbidden")]
    [InlineData("Not yours", "Ask its owner.", "")]
    public void A_refusal_without_a_title_a_detail_or_a_code_cannot_be_made(string title, string detail, string code) =>
        Assert.Throws<ArgumentException>(() => Refusals.Refuse(StatusCodes.Status403Forbidden, title, code, detail));

    [Fact]
    [Trait("criterion", "AC-refusal-format-2")]
    public async Task Each_mistake_has_a_code_a_message_a_fix_and_at_when_given_and_the_detail_counts_them()
    {
        var (response, body) = await SendAsync(HttpMethod.Put);
        var mistakes = body.GetProperty("mistakes").EnumerateArray().ToList();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("note.invalid", body.GetProperty("code").GetString());
        Assert.Equal("2 mistakes in `mistakes`: fix each as its `fix` says, then try again.", body.GetProperty("detail").GetString());
        Assert.Equal(
            [
                ("note.titleMissing", "The note has no title.", "Give it a title.", "title"),
                ("note.linkBroken", "The note links to procedure 'refund', which does not exist.", "Link an existing procedure.", null),
            ],
            mistakes.Select(mistake =>
                (
                    mistake.GetProperty("code").GetString(),
                    mistake.GetProperty("message").GetString(),
                    mistake.GetProperty("fix").GetString(),
                    mistake.TryGetProperty("at", out var at) ? at.GetString() : null
                )
            )
        );
    }

    [Fact]
    [Trait("criterion", "AC-refusal-format-2")]
    public void One_mistake_is_counted_in_the_singular() =>
        Assert.Equal("1 mistake in `mistakes`: fix each as its `fix` says, then try again.", Refusals.DetailOf(1));

    [Theory]
    [Trait("criterion", "AC-refusal-format-2")]
    [InlineData("", "The note has no title.", "Give it a title.")]
    [InlineData("note.titleMissing", "", "Give it a title.")]
    [InlineData("note.titleMissing", "The note has no title.", " ")]
    public void A_mistake_without_a_code_a_message_or_a_fix_cannot_be_sent(string code, string message, string fix) =>
        Assert.Throws<ArgumentException>(() =>
            Refusals.Refuse(
                StatusCodes.Status422UnprocessableEntity,
                "The note cannot be saved",
                "note.invalid",
                [new Mistake<string>(code, message, fix)]
            )
        );

    [Fact]
    [Trait("criterion", "AC-refusal-format-2")]
    public void A_list_of_no_mistakes_is_refused_with_the_call_that_fits() =>
        Assert.Contains(
            "Refuse(status, title, code, detail)",
            Assert
                .Throws<ArgumentException>(() =>
                    Refusals.Refuse(
                        StatusCodes.Status422UnprocessableEntity,
                        "The note cannot be saved",
                        "note.invalid",
                        Array.Empty<Mistake<string>>()
                    )
                )
                .Message,
            StringComparison.Ordinal
        );

    [Fact]
    [Trait("criterion", "AC-refusal-format-3")]
    public async Task A_mistakes_own_members_reach_the_client_unchanged_beside_the_shared_ones()
    {
        var (_, body) = await SendAsync(HttpMethod.Put);
        var broken = body.GetProperty("mistakes")[1];

        Assert.Equal("push", broken.GetProperty("stage").GetString(), StringComparer.OrdinalIgnoreCase);
        Assert.True(
            JsonNode.DeepEquals(
                JsonNode.Parse("""[{ "kind": "procedure", "id": "refund" }]"""),
                JsonNode.Parse(broken.GetProperty("related").GetRawText())
            )
        );
        Assert.Equal("note.linkBroken", broken.GetProperty("code").GetString());
    }

    [Fact]
    [Trait("criterion", "AC-refusal-format-4")]
    public async Task An_endpoint_that_can_refuse_has_the_refusal_shape_in_the_document()
    {
        var document = await DocumentAsync();
        var refusal = ResponseSchema(document, "put", 422);
        var mistake = Resolved(document, refusal?["properties"]?["mistakes"]?["items"]);

        Assert.Contains("code", Required(refusal));
        Assert.Contains("title", Required(refusal));
        Assert.Contains("detail", Required(refusal));
        Assert.DoesNotContain("mistakes", Required(refusal));
        Assert.Superset(new HashSet<string?> { "code", "message", "fix" }, Required(mistake).ToHashSet());
        Assert.DoesNotContain("at", Required(mistake));
        Assert.NotNull(mistake?["properties"]?["stage"]);
        Assert.Contains("code", Required(ResponseSchema(document, "post", 409)));
        Assert.Equal("string", ResponseSchema(document, "delete", 403)?["properties"]?["code"]?["type"]?.GetValue<string>());
    }

    [Fact]
    [Trait("criterion", "AC-refusal-format-5")]
    public async Task The_apps_codes_are_the_enum_of_code_and_of_each_mistakes_code()
    {
        var document = await DocumentAsync();
        var refusal = ResponseSchema(document, "put", 422);
        var mistake = Resolved(document, refusal?["properties"]?["mistakes"]?["items"]);
        string[] codes = ["note.invalid", "note.stale", "note.titleMissing", "note.linkBroken"];

        Assert.Equal(codes, EnumOf(document, refusal?["properties"]?["code"]));
        Assert.Equal(codes, EnumOf(document, mistake?["properties"]?["code"]));
        Assert.Empty(EnumOf(document, ResponseSchema(document, "delete", 403)?["properties"]?["code"]));
    }

    [Theory]
    [Trait("criterion", "AC-refusal-format-6")]
    [InlineData("/api/fail")]
    [InlineData("/api/nowhere")]
    public async Task A_failure_that_is_not_a_refusal_carries_no_code(string path)
    {
        var (response, body) = await SendAsync(HttpMethod.Get, path);

        Assert.True((int)response.StatusCode >= 400);
        Assert.Equal(Problem, response.Content.Headers.ContentType?.MediaType);
        Assert.False(body.TryGetProperty("code", out _));
        Assert.False(body.TryGetProperty("mistakes", out _));
    }
}
