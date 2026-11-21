using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Suwbackkit.Server.Tests;

public sealed class ContractSchemasTests
{
    [Fact]
    public async Task A_property_always_written_and_never_null_is_required_and_not_nullable()
    {
        var shelf = (await SchemasAsync(contractSchemas: true))["Shelf"];

        Assert.Contains("label", Required(shelf));
        Assert.Contains("tags", Required(shelf));
        Assert.Equal(["string"], Types(shelf?["properties"]?["label"]));
        Assert.Equal(["array"], Types(shelf?["properties"]?["tags"]));
    }

    [Fact]
    public async Task Calling_them_again_as_alfusion_did_changes_nothing()
    {
        var once = await SchemasAsync(contractSchemas: true);
        await using var server = await TestApp.StartAsync(
            builder => builder.Services.AddOpenApi(options => options.AddContractSchemas()),
            MapShelf
        );
        var json = await server.Client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.True(JsonNode.DeepEquals(once, JsonNode.Parse(json)?["components"]?["schemas"]));
    }

    [Fact]
    public async Task A_property_that_may_be_null_stays_optional_and_nullable()
    {
        var shelf = (await SchemasAsync(contractSchemas: true))["Shelf"];

        Assert.DoesNotContain("hint", Required(shelf));
        Assert.Equal(["null", "string"], Types(shelf?["properties"]?["hint"]));
    }

    [Fact]
    public async Task A_union_requires_its_discriminator_in_every_variant()
    {
        var schemas = await SchemasAsync(contractSchemas: true);

        Assert.Contains("kind", Required(schemas["ShapeCircle"]));
        Assert.Contains("kind", Required(schemas["ShapeSquare"]));
    }

    [Fact]
    public async Task Null_is_no_value_of_an_enum_and_a_nullable_property_still_says_null()
    {
        var schemas = await SchemasAsync(contractSchemas: true);

        Assert.Equal(["Matt", "Gloss"], schemas["Finish"]?["enum"]?.AsArray().Select(value => value?.GetValue<string>()));
        Assert.Contains(schemas["Shelf"]?["properties"]?["finish"]?["oneOf"]?.AsArray() ?? [], one => Types(one).SequenceEqual(["null"]));
    }

    [Fact]
    public async Task An_app_that_opts_out_gets_the_schemas_as_aspnet_core_writes_them()
    {
        var schemas = await SchemasAsync(contractSchemas: false);

        Assert.DoesNotContain("label", Required(schemas["Shelf"]));
        Assert.DoesNotContain("kind", Required(schemas["ShapeCircle"]));
        Assert.Contains(schemas["Finish"]?["enum"]?.AsArray() ?? [], value => value is null);
    }

    private static IEnumerable<string?> Required(JsonNode? schema) =>
        schema?["required"]?.AsArray().Select(name => name?.GetValue<string>()) ?? [];

    private static IEnumerable<string?> Types(JsonNode? schema) =>
        schema?["type"] switch
        {
            JsonArray many => many.Select(type => type?.GetValue<string>()),
            JsonValue one => [one.GetValue<string>()],
            _ => [],
        };

    private static async Task<JsonObject> SchemasAsync(bool contractSchemas)
    {
        await using var server = await TestApp.StartAsync(map: MapShelf, contractSchemas: contractSchemas);
        var json = await server.Client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative), TestContext.Current.CancellationToken);
        return JsonNode.Parse(json)?["components"]?["schemas"]?.AsObject() ?? throw new InvalidOperationException(json);
    }

    private static void MapShelf(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/shelf", () => new Shelf("Oak", null, Colour.Red, null, new Circle(1), null));
        routes.MapPost("/api/shelf", (Shelf shelf) => shelf);
        routes.MapGet("/api/shapes", () => new Shape[] { new Circle(1), new Square(2) });
    }

    [JsonConverter(typeof(JsonStringEnumConverter<Colour>))]
    internal enum Colour
    {
        Red,
        Green,
    }

    [JsonConverter(typeof(JsonStringEnumConverter<Finish>))]
    internal enum Finish
    {
        Matt,
        Gloss,
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [JsonDerivedType(typeof(Circle), "circle")]
    [JsonDerivedType(typeof(Square), "square")]
    internal abstract record Shape;

    internal sealed record Circle(double Radius) : Shape;

    internal sealed record Square(double Side) : Shape;

    internal sealed record Shelf(string Name, string? Note, Colour Colour, Colour? Trim, Shape Shape, Finish? Finish)
    {
        public string Label => Name.ToUpperInvariant();

        public string? Hint => Note;

        public IReadOnlyList<string> Tags => [Name];
    }
}
